# Asset View Design

## Context

This is sub-project #6, the last item of the Track B UI-completeness effort
(see `CLAUDE.md`, "Track B" roadmap): "A third top-level view, 'Asset
view' — sortable (type, name, length, timeline order, shooting order),
filterable (status, shot type, 'to shoot'), clicking an item opens the
full edit window from #3."

Sub-projects #1-#5 are done. Everything this view needs already exists on
the backend and in `ApiClientService` — no backend changes, no new API
routes. This is a frontend-only sub-project: one new top-level view, plus
a refactor that extracts Manage > Assets' existing inline edit form into
a reusable component, since "opens the full edit window from #3" means
literal reuse of that editor, not a re-implementation of it.

**Scope boundary:** a new route/component listing every asset in the
selected project (across all its episodes) in one sortable, filterable
table, where clicking a row opens the same asset-edit form Manage >
Assets already has, in a modal overlay.

Explicitly out of scope:

- Delete. The roadmap line for this view only mentions sorting,
  filtering, and opening the edit window — not row-level delete. Delete
  stays exclusively in Manage > Assets.
- Create. Same reasoning — Asset View is a browse/edit surface, not
  a creation surface.
- Any backend change. Every field this view sorts, filters, or edits by
  is already exposed through the existing REST API and `ApiClientService`.
- Bulk operations (multi-select, bulk status change, etc.) — not asked
  for.

## Refactor: extract `AssetEditorComponent`

`web/src/app/manage/assets-tab.component.ts`/`.html` currently has the
entire asset-edit form (12 editable fields, dynamic attribute key/value
rows, beat-link checkboxes, Save/Cancel) inline, as component state and
template on `AssetsTabComponent` itself (`editCode`, `editTitle`, ...,
`editBeatIds`, `addAttributeRow`, `removeAttributeRow`, `toggleBeatLink`,
`saveEdit`). This is the exact form Asset View needs to reuse — rather
than reimplementing it a second time (which would immediately create the
same DTO-mirroring-style drift risk this project has already been bitten
by twice), it moves into a new standalone `AssetEditorComponent`:

```ts
@Component({ selector: 'app-asset-editor', standalone: true, ... })
export class AssetEditorComponent {
  @Input({ required: true }) asset!: AssetDto;
  @Input() assetTypes: AssetTypeDto[] = [];
  @Input() phases: PhaseDto[] = [];
  @Input() beats: BeatDto[] = [];
  @Output() saved = new EventEmitter<AssetDto>();
  @Output() cancelled = new EventEmitter<void>();
  // ...editCode, editTitle, ..., editBeatIds, addAttributeRow,
  // removeAttributeRow, toggleBeatLink, save() — moved verbatim from
  // AssetsTabComponent, save() calls ApiClientService.updateAsset itself
  // and emits `saved` with the response instead of the caller doing it.
}
```

The component is self-contained: it owns the `updateAsset` call and
emits the updated `AssetDto` on success, so both call sites (Manage's
inline row and Asset View's modal) just listen for `(saved)`/`(cancelled)`
and don't duplicate any save logic.

`AssetsTabComponent` shrinks to: `editingId`, `startEdit(asset)` (just
sets `editingId`), `cancelEdit()`, and a new `onEditorSaved(updated:
AssetDto)` that replaces the row in `this.assets` and clears `editingId`
— the same post-save behavior `saveEdit` used to do inline. Its template
replaces the `<ng-template #editForm>` block with
`<app-asset-editor [asset]="asset" [assetTypes]="assetTypes"
[phases]="phases" [beats]="beats" (saved)="onEditorSaved($event)"
(cancelled)="cancelEdit()">`.

This is a behavior-preserving refactor of shipped, tested code — the
existing `AssetsTabComponent` tests for "starts an edit with the full
field set" and "saves an edit, rebuilding the attributes map" move to a
new `asset-editor.component.spec.ts`, adapted to the new inputs/outputs;
`assets-tab.component.spec.ts` keeps its list/create/delete tests and
gains one new test for `onEditorSaved` updating the row in place.

## `AssetViewComponent`

New route `/assets`, new nav link "Assets" (alongside Storyboard,
Production Plan, Manage — `app.routes.ts`, `app.html`, both desktop and
mobile nav).

**Data loading**, mirroring `ProductionPlanComponent`'s existing
per-episode `forkJoin` pattern: on selected-project change, fetch
episodes, then for each episode fetch its assets and beats in parallel
(`forkJoin`), plus `getAssetTypes()` (global) and `getPhases(projectId)`
(project-scoped, needed by the edit modal). Flattens into:
- `assets: AssetDto[]` — every asset across every episode in the project.
- `assetTypesById: Map<number, AssetTypeDto>`.
- `beatsById: Map<number, BeatDto>` — every beat across every episode,
  keyed by id, used to resolve "timeline order" (below).
- `beatsByEpisode: Map<number, BeatDto[]>` — used to scope the edit
  modal's beat-link checkboxes to the clicked asset's own episode,
  exactly like Manage > Assets already does.
- `phases: PhaseDto[]`.

**Sorting.** Exactly the five dimensions the roadmap names — Type, Name,
Length, Timeline order, Shooting order — each with an ascending/descending
toggle on repeated clicks of the same column header:
- **Type** — `assetTypesById.get(asset.assetTypeId)?.name ?? ''`, string sort.
- **Name** — `asset.title`, string sort.
- **Length** — `asset.targetLengthSeconds ?? Number.MAX_SAFE_INTEGER`.
- **Timeline order** — the minimum `Ordinal` among the asset's linked
  beats (via `beatIds` → `beatsById`), or `Number.MAX_SAFE_INTEGER` if
  unlinked. Mirrors how Storyboard implicitly orders shots by their
  beat's position.
- **Shooting order** — `asset.sequenceNumber ?? Number.MAX_SAFE_INTEGER`
  — the same global page-number field Production Plan's "Seq" column
  shows, i.e. the project's actual shoot-day running order. (Not
  `orderInPhase`, which is phase-local and wouldn't produce a single
  global order across every phase.)

**Filtering**, exactly the three the roadmap names:
- **Status** — dropdown, `ASSET_STATUSES` plus an "All" option.
- **Shot type** — dropdown, `assetTypes` plus an "All" option (the same
  dimension "Type" sorts by, just as a filter here).
- **"To shoot"** — a checkbox. Defined as `asset.completedAtUtc === null`
  — the dedicated boolean-shaped field this app already has for exactly
  this purpose (`CLAUDE.md`: "`Asset.CompletedAtUtc` already exists…
  specifically so [shoot-day tooling] needs no migration"), rather than
  pattern-matching on the free-form `status` string.

Filtering and sorting are pure logic, extracted into
`web/src/app/asset-view/asset-view.logic.ts` (mirroring the
`phase-grouping.ts`/`timeline-model.ts` precedent of keeping list
transforms as plain, independently-testable functions rather than
inline component logic):

```ts
export type SortKey = 'type' | 'name' | 'length' | 'timeline' | 'shooting';
export type SortDir = 'asc' | 'desc';

export interface AssetViewFilters {
  status: string | null;       // null = All
  assetTypeId: number | null;  // null = All
  toShootOnly: boolean;
}

export function filterAndSortAssets(
  assets: AssetDto[],
  assetTypesById: Map<number, AssetTypeDto>,
  beatsById: Map<number, BeatDto>,
  filters: AssetViewFilters,
  sortKey: SortKey,
  sortDir: SortDir,
): AssetDto[] { /* ... */ }
```

**Row click → edit modal.** Clicking a row sets `selectedAssetForEdit:
AssetDto | null`. A fixed-position overlay (`bg-black/50` backdrop,
centered `bg-surface` panel — no modal primitive exists yet in this app,
so this introduces the pattern) renders
`<app-asset-editor [asset]="selectedAssetForEdit" [assetTypes]="assetTypes"
[phases]="phases" [beats]="beatsByEpisode.get(selectedAssetForEdit.episodeId) ?? []"
(saved)="onEditorSaved($event)" (cancelled)="selectedAssetForEdit = null">`
when non-null. `onEditorSaved` replaces the asset in `this.assets`
in place and closes the modal. Clicking the backdrop also closes it
(same effect as Cancel).

**Error handling.** No new scheme — matches every existing mutation in
this app (`.subscribe(updated => ...)` with no error callback).

## Testing

- `asset-editor.component.spec.ts` (new): the two tests moved from
  `assets-tab.component.spec.ts` ("starts an edit with the full field
  set…", "saves an edit, rebuilding the attributes map…"), adapted to
  the component's own inputs/outputs instead of a parent's fields.
- `assets-tab.component.spec.ts`: keeps its list/create/delete/episode-
  scoping tests; gains one test asserting `onEditorSaved` replaces the
  row and clears `editingId`.
- `asset-view.logic.spec.ts` (new): unit tests for `filterAndSortAssets`
  covering each of the 5 sort keys (including the null-fallback cases)
  and each of the 3 filters, plus combinations.
- `asset-view.component.spec.ts` (new): loads data scoped to the project
  (mirrors `ProductionPlanComponent`'s "merges assets from every episode"
  test shape), clicking a row opens the modal with the right asset/beats,
  `onEditorSaved` updates the row and closes the modal.

## Deferred / explicitly not built here

- Row-level delete, and asset creation, from this view.
- Bulk operations.
- Persisting sort/filter state across navigation (resets on revisit —
  no requirement was stated for persistence).
