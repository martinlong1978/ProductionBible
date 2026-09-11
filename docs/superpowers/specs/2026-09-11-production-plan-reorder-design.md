# Production Plan CRUD + Reorder Design

## Context

This is sub-project #5 of the Track B UI-completeness effort (see
`CLAUDE.md`, "Track B" roadmap): "Production Plan CRUD + reorder — Phase
CRUD, phases and shots within a phase orderable, mirroring #4 but for the
shoot-day plan." Phase CRUD itself already shipped in sub-project #3
(`2026-09-11-generic-crud-ui-design.md`, Manage > Phases tab) — this spec
covers what's left: reordering phases, and reordering shots within a
phase, on the read-only Production Plan view
(`web/src/app/production-plan/`).

**Two real bugs, discovered while scoping this work, must be fixed as
part of it — they are not scope creep, they're on the exact files this
sub-project already has to touch:**

1. **GitHub issue #18** (filed during the ordering-infrastructure final
   review, still open): `AssetService.ReorderWithinPhaseAsync`
   (`src/ProductionBible.Application/Services/AssetService.cs:105-117`)
   writes its within-phase position into `Asset.SequenceNumber` — the
   *global* 1-65 page number the importer assigns from the source
   production plan, rendered as the "Seq" column on this exact view. Every
   phase reorder through this endpoint overwrites those numbers with a
   local `0..n-1` range, colliding with every other phase's numbers and
   silently destroying the original imported page order — with no way
   back short of a full reimport. Not reachable from any UI today (this
   spec is what makes it reachable), so fixing it is a precondition, not
   an afterthought.

2. **Newly discovered, not yet filed as an issue (fixed directly here
   instead — same files this sub-project already rewrites):**
   `web/src/app/production-plan/phase-grouping.ts` groups assets by
   `asset.attributes['PhaseGroup']`, a string attribute the importer
   stopped writing when Phase became a first-class entity
   (`src/ProductionBible.Importer/ImportMapper.cs:65` sets `asset.Phase`
   directly now, via `GetOrCreatePhase` — no `AddAttribute(asset,
   "PhaseGroup", ...)` call exists anymore). On any database imported
   since that migration (PR #23), every asset's `attributes['PhaseGroup']`
   is absent, so `buildPhaseGroups` buckets every single asset into
   "Unphased." **This is a live, currently-shipped regression on a page
   used for actual shoot days**, not a missing feature — the Phase-entity
   migration's frontend counterpart for this view was never written.

**Scope boundary:** backend (one new nullable column + a service-method
fix) and frontend (rewire Production Plan's phase grouping to the real
`Phase` entity, then add CDK drag-reorder for phases and for shots within
a phase). Phase CRUD forms, Asset CRUD forms, and Asset's phase-dropdown
assignment already exist (Manage, sub-project #3) — untouched here.

Explicitly out of scope:

- The Importer is not changed. `Asset.OrderInPhase` (the new field) is
  left null by the importer; ordering falls back to `SequenceNumber`
  (see "Ordering fallback" below), so existing seeded databases don't
  need a reimport to benefit from this feature.
- Storyboard's beat/shot reorder (sub-project #4, already shipped) is
  untouched.
- The "Seq" column keeps showing `asset.sequenceNumber` unchanged — a
  manual phase reorder may make it visually non-sequential within a
  phase afterward (the same category of cosmetic consequence as item 04's
  M-6 finding for beat timecodes); a follow-up GitHub issue will be filed
  for this, not solved here.
- Sub-project #6 (Asset view, a new sortable/filterable top-level view)
  is separate work.

## Backend: fix the reorder-within-phase corruption (issue #18)

Add `Asset.OrderInPhase` (`int?`, nullable — mirrors `AssetBeat.OrderInBeat`'s
type and nullability exactly) via a new EF Core migration
(`dotnet ef migrations add AddOrderInPhaseToAsset --project
src/ProductionBible.Application --startup-project src/ProductionBible.Api`).
`Program.cs` already calls `db.Database.Migrate()` on every startup
(`src/ProductionBible.Api/Program.cs:41`), so existing seeded databases
pick up the new nullable column automatically — no manual reseed step.

`AssetService.ReorderWithinPhaseAsync` changes its one assignment line
from `assetsById[orderedAssetIds[i]].SequenceNumber = i;` to
`assetsById[orderedAssetIds[i]].OrderInPhase = i;` — otherwise unchanged
(same validation: count match, no duplicates, all ids belong to the
phase).

`AssetDto` gains `OrderInPhase` (`int?`), appended as the last positional
parameter (the only construction site is `AssetService.ToDto`, so
appending avoids touching any other call site). `CreateAssetRequest`/
`UpdateAssetRequest` do **not** gain a matching field — like
`AssetBeat.OrderInBeat`, this is server-managed via the reorder endpoint
only, never client-set through create/update.

**Ordering fallback:** wherever assets within a phase need an initial
order — before any explicit reorder has ever happened — use
`OrderInPhase ?? SequenceNumber ?? int.MaxValue`, the exact fallback
pattern `BeatService.GetByEpisodeAsync` already uses for
`OrderInBeat ?? int.MaxValue` (`BeatService.cs:37`). This sort happens
client-side (see below) since Production Plan already fetches raw asset
lists and groups them client-side today — no new backend endpoint needed
for reading.

## Frontend: fix phase grouping to use the real `Phase` entity

`phase-grouping.ts` is rewritten:

```ts
export interface PhaseGroup {
  phaseId: number | null;
  phase: string;
  assets: AssetDto[];
}

export function buildPhaseGroups(assets: AssetDto[], phases: PhaseDto[]): {
  phaseGroups: PhaseGroup[];
  unphasedGroup: PhaseGroup | null;
} {
  const byPhase = new Map<number, AssetDto[]>();
  const unphased: AssetDto[] = [];
  for (const asset of assets) {
    if (asset.phaseId === null) {
      unphased.push(asset);
      continue;
    }
    if (!byPhase.has(asset.phaseId)) byPhase.set(asset.phaseId, []);
    byPhase.get(asset.phaseId)!.push(asset);
  }

  const sortAssets = (list: AssetDto[]) =>
    [...list].sort((a, b) =>
      (a.orderInPhase ?? a.sequenceNumber ?? Number.MAX_SAFE_INTEGER) -
      (b.orderInPhase ?? b.sequenceNumber ?? Number.MAX_SAFE_INTEGER));

  const phaseGroups = [...phases]
    .sort((a, b) => a.orderIndex - b.orderIndex)
    .map((phase) => ({
      phaseId: phase.id,
      phase: phase.name,
      assets: sortAssets(byPhase.get(phase.id) ?? []),
    }));

  const unphasedGroup = unphased.length > 0
    ? { phaseId: null, phase: 'Unphased', assets: sortAssets(unphased) }
    : null;

  return { phaseGroups, unphasedGroup };
}
```

This replaces the old single-array return with a `{ phaseGroups,
unphasedGroup }` pair — deliberately mirroring `BibleComponent`'s
`beats` (reorderable) / `unassignedAssets` (static, not reorderable)
split, since the same asymmetry applies here: only real phases are
draggable, "Unphased" is a fixed bucket with no reorder endpoint to call.
A phase with zero currently-linked assets still appears (empty group) —
Manage > Phases already allows creating a phase before assigning any
asset to it, and hiding empty phases here would make a freshly created
phase invisible on this view until an asset is assigned, which is
confusing rather than helpful.

`ProductionPlanComponent` fetches `getPhases(projectId)` alongside the
per-episode asset fetch (`forkJoin` already used for episodes' assets;
`getPhases` joins the same `forkJoin` or a sibling one), and calls
`buildPhaseGroups(assetLists.flat(), phases)`, storing `phaseGroups` and
`unphasedGroup` as two component fields (replacing today's single
`groups` field).

## Frontend: drag-reorder

Two independent `cdkDropList`s, same pattern as sub-project #4:

1. **Phase reorder** — `phaseGroups` wrapped in an outer `cdkDropList`;
   dropping calls `PATCH /api/projects/{projectId}/phases/reorder`
   (endpoint already exists and is already correct — `PhaseService.
   ReorderAsync` writes `Phase.OrderIndex`, a dedicated field, not
   `SequenceNumber` — no backend fix needed here, only issue #18's
   asset-within-phase endpoint was broken). The `unphasedGroup` section
   sits outside this drop list, exactly like Storyboard's "Unassigned"
   section — never reorderable, never draggable.
2. **Shot reorder within a phase** — each phase group's asset list
   becomes its own `cdkDropList` (one per phase, not connected to each
   other); dropping calls `PATCH /api/phases/{phaseId}/assets/reorder`
   (now fixed to write `OrderInPhase`).

**Guards baked in from the start** (sub-project #4's final review found
these missing and fixed them in a follow-up round — this time they're
part of the initial implementation, not rediscovered):
- `if (!event.isPointerOverContainer) return;` at the top of both drop
  handlers — an attempted cross-phase drag must not silently persist a
  sort within the source phase.
- `if (event.previousIndex === event.currentIndex) return;` right after —
  a no-op drop (grab-and-release with no movement) must not fire a
  needless PATCH and reload.
- The within-phase handler derives `orderedIds` from the *rendered* list
  (the group's own `assets` array, which — unlike Storyboard's
  `assetsForBeat()` — is a real stored array on the component's group
  object, not a derived-on-every-call method, so this class of bug from
  sub-project #4's I-2 doesn't apply the same way here; still, the ids
  sent must be exactly the ids rendered, never a separately-fetched or
  stale set).
- `markForCheck()` after each optimistic mutation, before the HTTP call —
  this app is zoneless (no `provideZoneChangeDetection`); this exact
  omission has shipped invisible-until-refresh bugs twice already in this
  project (sub-project #3, and initially in sub-project #4 before its
  final review).

**Interaction, auto-save, and reconciliation** — identical to sub-project
#4: drag handles (not whole-row drag, since the Production Plan view is
a `<table>`, not a card list — the handle becomes a new narrow first
`<td>` column, with a matching narrow empty header cell), auto-save on
drop with no Save button, optimistic move, reload-on-response to
reconcile with the server (`loadGroups(projectId)`, the component's
existing private reload method, re-fetches phases + assets and rebuilds
both group lists).

## API client additions

```ts
reorderPhases(projectId: number, orderedIds: number[]): Observable<void> {
  return this.http.patch<void>(`/api/projects/${projectId}/phases/reorder`, { orderedIds });
}

reorderAssetsWithinPhase(phaseId: number, orderedIds: number[]): Observable<void> {
  return this.http.patch<void>(`/api/phases/${phaseId}/assets/reorder`, { orderedIds });
}
```

## models.ts additions

```ts
// AssetDto gains:
orderInPhase: number | null;
```

(`PhaseDto` already exists from sub-project #3, unchanged.)

## Error handling

No new error-handling scheme — matches every existing mutation in this
app (`.subscribe(success => ...)` with no error callback), same as
sub-project #4.

## Testing

- **Backend:** `AssetServiceTests.cs`'s existing
  `ReorderWithinPhaseAsync_reassigns_SequenceNumber_to_match_the_given_order`
  test currently documents the bug — it gets replaced with
  `ReorderWithinPhaseAsync_reassigns_OrderInPhase_to_match_the_given_order`
  (asserting `OrderInPhase`, not `SequenceNumber`) plus a new test
  asserting `SequenceNumber` is left untouched by a reorder. The existing
  duplicate-id rejection test
  (`ReorderWithinPhaseAsync_rejects_a_duplicate_id_and_writes_nothing`)
  is updated to assert `OrderInPhase` is unchanged rather than
  `SequenceNumber` (still correct either way since nothing should change
  on rejection, but the assertion should track the field the method
  actually writes).
- **Frontend:** `phase-grouping.spec.ts`-equivalent (currently tests live
  inside `production-plan.component.spec.ts`) tests are rewritten for the
  new `phaseId`/`PhaseDto`-based grouping and the `{ phaseGroups,
  unphasedGroup }` return shape, replacing the `attributes['PhaseGroup']`
  fixtures. `ApiClientService` tests: two new tests for `reorderPhases`/
  `reorderAssetsWithinPhase`, mirroring sub-project #4's pattern.
  `ProductionPlanComponent` tests: two new tests for the drop handlers,
  calling them directly with constructed `CdkDragDrop`-shaped events
  (`previousIndex`/`currentIndex`/`isPointerOverContainer`), asserting
  the right reorder call fires with the right id array — no test
  exercises CDK's actual pointer/drag mechanics, same as sub-project #4.

## Deferred / explicitly not built here

- Reimporting existing data to backfill `Asset.OrderInPhase` from the
  source document's original within-phase order — the `SequenceNumber`
  fallback covers initial ordering well enough without one.
- A follow-up issue for the "Seq" column becoming visually
  non-monotonic within a phase after a manual reorder (mirrors item 04's
  M-6, filed as issue #25, for the same reason).
- Sub-project #6 (Asset view).
