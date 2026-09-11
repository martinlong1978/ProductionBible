# Generic CRUD UI Design

## Context

This is sub-project #3 of the Track B UI-completeness effort (see
`CLAUDE.md`, "Track B" roadmap). Sub-project #2, ordering infrastructure
(`2026-09-11-ordering-infrastructure-design.md`), shipped full backend CRUD +
reorder endpoints for every entity but no UI calls any of it yet except
`Asset`'s Status/Notes quick-edit on the Storyboard view.

**Scope boundary:** backend already has full CRUD for every entity in scope
(`Project`, `Episode`, `Phase`, `Beat`, `Asset` — verified against
`I*Service.cs` in `src/ProductionBible.Application/Services/`). This is a
**frontend-only** change: new Angular components, new `ApiClientService`
methods, and `models.ts` fixes. No backend/schema changes.

Also fixed as a byproduct: `web/src/app/core/models.ts`'s `BeatDto` is
already stale against the backend record — missing `Ordinal`,
`DurationSeconds`, `StartSeconds`, `EndSeconds` (present on the server's
`BeatDto` since ordering-infrastructure). This is the exact DTO-mirroring gap
CLAUDE.md warns about (a prior instance of it caused the PhaseId data-loss
bug fixed in commit `736d620`). The Beat CRUD form needs `Ordinal` and
`DurationSeconds` fields regardless (`CreateBeatRequest`/`UpdateBeatRequest`
require both), so fixing the TS type is required by this work, not optional
cleanup.

Explicitly out of scope: AssetType management (Asset's editor only needs to
*list* asset types for a dropdown — no create/edit/delete UI for them, since
Track B's item list never names `AssetType`), reorder drag-and-drop (that's
sub-project #4/#5), and any change to Storyboard's existing inline
Status/Notes quick-edit.

## Routing and navigation

New route `/manage`, new `ManageComponent`. Nav gets a third link,
"Manage", alongside "Storyboard" and "Production Plan"
(`web/src/app/app.html`, `web/src/app/app.routes.ts`).

`ManageComponent` renders an in-page tab strip: **Projects / Episodes /
Phases / Beats / Assets**. One route, five child presentational components
swapped by tab state (`activeTab` signal or field) — matches the app's
existing single-route-per-view pattern (Storyboard, Production Plan are each
one route).

- **Projects tab:** lists all projects (global, no scoping).
- **Episodes tab:** scoped to the globally-selected project
  (`ProjectContextService.selectedProjectId()`, already used app-wide).
- **Phases tab:** scoped to the globally-selected project.
- **Beats tab:** needs an episode picker inside the tab (reuse the
  `<select>` pattern from `bible.component.html`'s episode selector) —
  Manage has no global episode selection.
- **Assets tab:** same episode picker as Beats.

## API client additions

`web/src/app/core/api-client.service.ts` currently has GET for
projects/episodes/beats/assets/asset-types, plus one `updateAsset` PUT.
Adding:

```ts
// Projects
createProject(request: CreateProjectRequest): Observable<ProjectDto>;
updateProject(id: number, request: UpdateProjectRequest): Observable<ProjectDto>;
deleteProject(id: number): Observable<void>;

// Episodes
createEpisode(projectId: number, request: CreateEpisodeRequest): Observable<EpisodeDto>;
updateEpisode(id: number, request: UpdateEpisodeRequest): Observable<EpisodeDto>;
deleteEpisode(id: number): Observable<void>;

// Phases
getPhases(projectId: number): Observable<PhaseDto[]>;
createPhase(projectId: number, request: CreatePhaseRequest): Observable<PhaseDto>;
updatePhase(id: number, request: UpdatePhaseRequest): Observable<PhaseDto>;
deletePhase(id: number): Observable<void>;

// Beats
createBeat(episodeId: number, request: CreateBeatRequest): Observable<BeatDto>;
updateBeat(id: number, request: UpdateBeatRequest): Observable<BeatDto>;
deleteBeat(id: number): Observable<void>;

// Assets
createAsset(episodeId: number, request: CreateAssetRequest): Observable<AssetDto>;
deleteAsset(id: number): Observable<void>;
```

Routes follow the existing REST shape used by the backend controllers
(`/api/projects`, `/api/projects/{id}/episodes`, `/api/projects/{id}/phases`,
`/api/episodes/{id}/beats`, `/api/episodes/{id}/assets`, `/api/assets/{id}`,
etc. — confirm exact paths against each `*Controller.cs` during
implementation).

## models.ts additions/fixes

```ts
export interface PhaseDto {
  id: number;
  projectId: number;
  name: string;
  orderIndex: number;
}
export interface CreatePhaseRequest { name: string; orderIndex: number; }
export interface UpdatePhaseRequest { name: string; orderIndex: number; }

export interface CreateEpisodeRequest { name: string; orderIndex: number; }
export interface UpdateEpisodeRequest { name: string; orderIndex: number; }

export interface CreateProjectRequest { name: string; description: string | null; }
export interface UpdateProjectRequest { name: string; description: string | null; }

export interface CreateAssetRequest {
  assetTypeId: number;
  code: string;
  title: string;
  scriptText: string | null;
  status: string;
  notes: string | null;
  sequenceNumber: number | null;
  targetLengthSeconds: number | null;
  phaseId: number | null;
  attributes: Record<string, string> | null;
  beatIds: number[] | null;
}

export interface CreateBeatRequest {
  timecode: string;
  purpose: string;
  ordinal: number;
  durationSeconds: number;
}
export interface UpdateBeatRequest {
  timecode: string;
  purpose: string;
  ordinal: number;
  durationSeconds: number;
}
```

Fix `BeatDto` to match the backend record exactly:

```ts
export interface BeatDto {
  id: number;
  episodeId: number;
  timecode: string;
  purpose: string;
  ordinal: number;          // NEW
  durationSeconds: number;  // NEW
  startSeconds: number;     // NEW
  endSeconds: number;       // NEW
  assetIds: number[];
}
```

Note: this spec does not change `timeline-model.ts` to consume the new
fields — that re-derivation (and its known divergence, issue #22) is a
separate, already-tracked concern. Adding the fields to the TS type here
only unblocks the Beat CRUD form; nothing in this change requires
`timeline-model.ts` to be touched.

## Shared UI patterns

Three patterns get built once and reused across all five tabs:

1. **Row list.** A simple list/table: one identifying column (name/code),
   an Edit button, a Delete button. Reused per entity with different
   identifying-column content.
2. **In-page delete confirm.** No native `confirm()`. Clicking Delete turns
   the row's action area into "Really delete?" + Cancel; a second click on
   "Really delete?" fires the delete call. Matches the existing Save/Cancel
   inline-edit pattern already used on Storyboard, and keeps the feature
   verifiable by browser automation afterward (native dialogs block
   automated verification).
3. **Full edit form.** Label + input rows, Save/Cancel buttons at the
   bottom — same visual pattern as Storyboard's existing inline editor,
   just with more fields per entity. One form template per entity (fields
   differ), not one generic reflective form — entity field lists are
   small and fixed, a generic form-builder would be over-engineering for
   five known shapes.

## Per-entity fields

| Entity  | Fields in create/edit form | Parent scope |
|---------|----------------------------|--------------|
| Project | Name, Description | none (global) |
| Episode | Name, OrderIndex | Project (fixed = current selection) |
| Phase   | Name, OrderIndex | Project (fixed = current selection) |
| Beat    | Timecode, Purpose, Ordinal, DurationSeconds | Episode (fixed = picker selection) |
| Asset   | **Edit form:** AssetType (dropdown), Code, Title, ScriptText, Status (dropdown, reuse `ASSET_STATUSES`), Notes, SequenceNumber, TargetLengthSeconds, Phase (dropdown, scoped to project), Attributes (key/value pairs, add/remove rows), Beat links (multi-select from the episode's beats). **Create form:** Code, Title, AssetType only — `Status` defaults to `"Planned"`, everything else nulls; the full field set is reachable via a follow-up Edit. This matches the app's existing pattern of a lightweight create plus a richer edit (see Beat/Episode/Phase, which also create with fewer fields than they can later edit). | Episode (fixed = picker selection) |

Asset's Beat-links multi-select **is** the "AssetBeat link management UI"
Track B item #3 calls for — no separate screen. `UpdateAssetRequest.BeatIds`
/ `CreateAssetRequest.BeatIds` already round-trip the full set of links
(confirmed in `AssetService.cs`'s `ApplyBeatLinks`); the Manage > Assets
full editor is simply the first UI surface that exposes editing that array
directly (Storyboard's quick-edit does not touch beat links).

## Error handling

No error handling exists anywhere in the app yet (issue #3, pre-existing,
explicitly out of scope for this change). New CRUD calls follow the
existing no-op-on-http-error pattern already used by
`bible.component.ts`'s `saveEdit` — `.subscribe(success => ...)` with no
error callback. Not introducing a parallel error-handling scheme for one
feature; that's issue #3's job when it's picked up.

## Testing

TDD per component, following the existing spec pattern in
`bible.component.spec.ts` / `production-plan.component.spec.ts`: for each
of the five tabs, test list rendering, create, edit (field round-trip through
`ApiClientService`), delete (including the two-step in-page confirm), and
parent-scoping (e.g. switching the selected project reloads the Episodes
tab). No backend tests — no backend changes in this spec.

## Deferred / explicitly not built here

- Drag-reorder UI for any entity (sub-project #4: Storyboard reorder,
  sub-project #5: Production Plan reorder — both build on the reorder
  endpoints ordering-infrastructure already shipped).
- AssetType create/edit/delete UI.
- Any change to `timeline-model.ts`'s client-side timecode re-derivation
  (issue #22).
- App-wide HTTP error handling (issue #3).
