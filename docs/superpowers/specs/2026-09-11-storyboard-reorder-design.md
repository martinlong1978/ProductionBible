# Storyboard Reorder Workflow Design

## Context

This is sub-project #4 of the Track B UI-completeness effort (see
`CLAUDE.md`, "Track B" roadmap). It builds directly on the reorder
endpoints ordering-infrastructure shipped
(`2026-09-11-ordering-infrastructure-design.md`): `PATCH
/api/episodes/{episodeId}/beats/reorder` and `PATCH
/api/beats/{beatId}/asset-beats/reorder` both exist and are tested
server-side, but nothing in the UI calls either one yet.

**Scope boundary:** frontend-only, one view. Adds drag-to-reorder to the
Storyboard List view (`web/src/app/bible/bible.component.ts`/`.html`) for
two independent scopes:

1. **Beats within an episode** — reordering the `<section>` blocks in the
   List view. Calls the beats-reorder endpoint.
2. **Shots (assets) within a beat** — reordering the `<li>` rows inside one
   beat's asset list. Calls the asset-beats-reorder endpoint. This does
   **not** move a shot to a different beat — that's beat-membership
   editing, already covered by Manage > Assets' beat-links checkboxes
   (`2026-09-11-generic-crud-ui-design.md`).

Explicitly out of scope:

- The Storyboard **Timeline** view mode stays read-only. It's a computed
  visualization (`timeline-model.ts` derives rows from `Ordinal`/
  `startSeconds`/`endSeconds`), not an editable list — dragging inside a
  time-scaled view is a different, harder interaction problem than a plain
  list, and the plan doesn't call for it.
- The Storyboard's "Unassigned" section (assets with no beat link) is not
  reorderable — no reorder endpoint applies to an asset with no `BeatId`,
  and nothing in the spec calls for one.
- Production Plan's own reorder workflow (Phase CRUD, phase/shot ordering)
  is sub-project #5, a separate spec.
- Cross-beat drag (moving a shot from one beat to another via drag) is out
  of scope — see point 2 above.

## New dependency

`@angular/cdk` (`^22.1.0`, matching the installed `@angular/core` version)
for `DragDropModule` (`CdkDropList`, `CdkDrag`, `CdkDragHandle`). This is
the standard Angular drag-and-drop primitive; no other project in this
repo hand-rolls HTML5 drag-and-drop, and CDK is the idiomatic choice for
an Angular app already on `@angular/core`.

## Interaction design

- **Drag handle, not whole-row drag.** Each beat section header and each
  asset row gets a small handle icon (`cdkDragHandle`) — dragging starts
  only from the handle, so the existing "Edit" button's click target stays
  unambiguous. (A whole-row-draggable list would make every click on Edit
  a potential drag-start, which CDK doesn't disambiguate for you.)
- **Auto-save on drop.** No explicit Save button, no pending/dirty state
  to track. CDK moves the DOM item immediately on drop (optimistic); the
  drop handler then fires the reorder HTTP call with the full new ordered
  ID array. This matches the app's existing pattern — every other mutation
  in this app (Storyboard's Status/Notes edit, every Manage tab) saves
  immediately on the triggering action, nothing in this codebase uses a
  batch-save pattern.
- **Reconciliation on response.** After the reorder call's response
  arrives, reload the affected list from the server
  (`selectEpisode(selectedEpisodeId)` for a beat reorder, a targeted
  `getBeats`/`getAssets` refresh for a shot reorder) so the client's view
  of `Ordinal`/`OrderInBeat` matches the server exactly, rather than
  trusting the optimistic client-side reorder to stay correct forever.
  Matches the existing no-error-handling convention: on failure, the
  reload is skipped and the optimistic (now-incorrect) order sits until
  the next natural reload — no error UI, consistent with every other
  mutation in this app.

## Component structure

`bible.component.html`'s List view currently wraps the beats loop in
`<ng-container *ngFor="let beat of beats">` — CDK's `cdkDropList` needs a
real DOM element as its host, not `<ng-container>`. The beats loop moves
to a `<div cdkDropList>` wrapping `<section *ngFor="let beat of beats"
cdkDrag>` elements. Each beat's own `<ul>` of assets becomes its own
`<ul cdkDropList>` (one per beat — **not** connected to each other via
`cdkDropListConnectedTo`, so a shot can only reorder within its own list,
never drop into a different beat's list), with each `<li cdkDrag>` inside.

`BibleComponent` gains:

```ts
onBeatDrop(event: CdkDragDrop<BeatDto[]>): void {
  moveItemInArray(this.beats, event.previousIndex, event.currentIndex);
  const orderedIds = this.beats.map((b) => b.id);
  this.api.reorderBeats(this.selectedEpisodeId!, orderedIds).subscribe(() => {
    this.selectEpisode(this.selectedEpisodeId!);
  });
}

onAssetDrop(beat: BeatDto, event: CdkDragDrop<AssetDto[]>): void {
  const assets = this.assetsForBeat(beat);
  moveItemInArray(assets, event.previousIndex, event.currentIndex);
  const orderedIds = assets.map((a) => a.id);
  this.api.reorderAssetsWithinBeat(beat.id, orderedIds).subscribe(() => {
    this.selectEpisode(this.selectedEpisodeId!);
  });
}
```

`moveItemInArray` is `@angular/cdk/drag-drop`'s own array-splice helper —
no hand-rolled array surgery needed.

## API client additions

`web/src/app/core/api-client.service.ts` gains two methods, matching the
backend routes exactly (`ReorderRequest` on the C# side takes
`{ OrderedIds: int[] }`, camelCased to `{ orderedIds: number[] }` over the
wire):

```ts
reorderBeats(episodeId: number, orderedIds: number[]): Observable<void> {
  return this.http.patch<void>(`/api/episodes/${episodeId}/beats/reorder`, { orderedIds });
}

reorderAssetsWithinBeat(beatId: number, orderedIds: number[]): Observable<void> {
  return this.http.patch<void>(`/api/beats/${beatId}/asset-beats/reorder`, { orderedIds });
}
```

## Error handling

No new error-handling scheme — matches every other mutation in this app.
On a failed reorder call, the reload in the `.subscribe()` success
callback simply doesn't run, leaving the optimistic (client-only) order in
place until the next natural reload (switching episodes and back, or a
page refresh) corrects it. This is a pre-existing, deliberate app-wide
constraint (see `2026-09-11-generic-crud-ui-design.md`'s "Error handling"
section) that this spec is not the place to revisit.

## Testing

- `api-client.service.spec.ts`: two new tests for `reorderBeats` /
  `reorderAssetsWithinBeat`, verifying the `PATCH` method, URL, and body
  shape (mirrors the existing tests for `updateAsset` etc.).
- `bible.component.spec.ts`: two new tests. One calls `onBeatDrop` with a
  synthetic `CdkDragDrop` event object (`{ previousIndex, currentIndex }`
  is all the handler reads) and asserts `reorderBeats` was called with the
  correctly-reordered ID array. One calls `onAssetDrop` similarly for the
  asset-reorder path. No test exercises CDK's actual pointer/drag
  mechanics — that's the library's own tested behavior, not this app's.

## Deferred / explicitly not built here

- Drag-reorder in the Timeline view.
- Cross-beat drag (moving a shot between beats via drag).
- Reordering the "Unassigned" section.
- Production Plan's reorder workflow (sub-project #5).
