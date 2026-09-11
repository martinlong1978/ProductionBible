# Storyboard Reorder Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add drag-to-reorder to the Storyboard List view — beats within an episode, and shots within a beat — using the reorder endpoints ordering-infrastructure already shipped.

**Architecture:** `@angular/cdk`'s `DragDropModule` provides two independent `cdkDropList`s in `bible.component.html`: one for the beat sections, one per beat for its shot list (not connected to each other). Drop handlers in `BibleComponent` mutate the bound arrays in place (optimistic move), fire the matching reorder HTTP call, then reload the episode's beats/assets on success to reconcile with the server.

**Tech Stack:** Angular 22 (zoneless), `@angular/cdk` `^22.1.0`, RxJS, Jasmine/Karma.

**Spec:** `docs/superpowers/specs/2026-09-11-storyboard-reorder-design.md`

## Global Constraints

- Frontend-only. No backend/schema changes — all four reorder endpoints already exist and are tested server-side.
- Two independent `cdkDropList`s: beats (outer), shots-within-one-beat (inner, one per beat, never connected to each other via `cdkDropListConnectedTo`). A shot never moves to a different beat via drag.
- Timeline view stays read-only. Storyboard's "Unassigned" section stays non-reorderable.
- Auto-save on drop, no explicit Save button. Optimistic move on drop, then reload the affected data on the HTTP response to reconcile with the server.
- No new error-handling scheme: `.subscribe(success => ...)` with no error callback, matching every existing mutation in this app. On failure, the reconciling reload simply doesn't fire.
- The app is zoneless (no `provideZoneChangeDetection`) — any state mutation from inside a `.subscribe()` callback, or from an event handler, that should show up on screen needs an explicit `this.cdr.markForCheck()` call. This has bitten this project before (a missing `markForCheck()` shipped invisible-until-refresh bugs in Track B item 3) — every task below that touches component state calls it out explicitly.
- Any change under `web/src/` needs the wwwroot rebuild (`cd web && npx ng build --output-path=../src/ProductionBible.Api/wwwroot`) as its own explicit step, verified by actually running `dotnet run` — this is Task 4, not optional polish.

---

## File Structure

- Modify: `web/package.json` — add `@angular/cdk` dependency.
- Modify: `web/src/app/core/api-client.service.ts` — two new reorder methods.
- Modify: `web/src/app/core/api-client.service.spec.ts` — tests for the two new methods.
- Modify: `web/src/app/bible/bible.component.ts` — `DragDropModule` import, `onBeatDrop`/`onAssetDrop` handlers.
- Modify: `web/src/app/bible/bible.component.spec.ts` — tests for the two new handlers.
- Modify: `web/src/app/bible/bible.component.html` — `cdkDropList`/`cdkDrag`/`cdkDragHandle` markup for beats and for each beat's shot list.
- Modify: `src/ProductionBible.Api/wwwroot/browser/**` — rebuilt frontend bundle (Task 4).

---

## Task 1: Add the `@angular/cdk` dependency

**Files:**
- Modify: `web/package.json`

**Interfaces:**
- Produces: `@angular/cdk` available for import as `@angular/cdk/drag-drop` in Task 2.

- [ ] **Step 1: Add the dependency**

Open `web/package.json` and add `@angular/cdk` to `dependencies`, right next to `@angular/core`, at the same version range:

```json
"@angular/cdk": "^22.1.0",
```

(Match whatever exact range `@angular/core` uses in the file at the time — copy its range verbatim rather than retyping it, in case it has drifted from `^22.1.0`.)

- [ ] **Step 2: Install**

Run: `cd web && npm install`

Expected: `@angular/cdk` appears in `web/node_modules/@angular/cdk` and in `web/package-lock.json`.

- [ ] **Step 3: Verify the build still succeeds**

Run: `cd web && npx ng build`

Expected: build succeeds (the dependency is unused so far — this just confirms the install didn't break anything).

- [ ] **Step 4: Commit**

```bash
cd web
git add package.json package-lock.json
git commit -m "Add @angular/cdk dependency for storyboard drag-reorder"
```

---

## Task 2: Add reorder methods to `ApiClientService`

**Files:**
- Modify: `web/src/app/core/api-client.service.ts`
- Modify: `web/src/app/core/api-client.service.spec.ts`

**Interfaces:**
- Produces: `ApiClientService.reorderBeats(episodeId: number, orderedIds: number[]): Observable<void>`, `ApiClientService.reorderAssetsWithinBeat(beatId: number, orderedIds: number[]): Observable<void>` — both consumed by Task 3's `BibleComponent`.
- Consumes: nothing new. Backend routes already exist and are confirmed exact:
  `PATCH /api/episodes/{episodeId}/beats/reorder` (`BeatsController.cs:49`) and
  `PATCH /api/beats/{beatId}/asset-beats/reorder` (`AssetsController.cs:56`), both taking
  `{ orderedIds: number[] }` (C#'s `ReorderRequest.OrderedIds`, camelCased over the wire) and
  returning `204 No Content`.

- [ ] **Step 1: Write the failing tests**

Add to `web/src/app/core/api-client.service.spec.ts`, after the existing `deleteAsset` test (before the closing `});` of the `describe` block):

```ts
  it('sends a PATCH to /api/episodes/{episodeId}/beats/reorder for reorderBeats', () => {
    service.reorderBeats(10, [102, 100, 101]).subscribe();
    const req = httpMock.expectOne('/api/episodes/10/beats/reorder');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ orderedIds: [102, 100, 101] });
    req.flush(null);
  });

  it('sends a PATCH to /api/beats/{beatId}/asset-beats/reorder for reorderAssetsWithinBeat', () => {
    service.reorderAssetsWithinBeat(100, [1002, 1000, 1001]).subscribe();
    const req = httpMock.expectOne('/api/beats/100/asset-beats/reorder');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ orderedIds: [1002, 1000, 1001] });
    req.flush(null);
  });
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd web && npx ng test --watch=false`

Expected: FAIL — `service.reorderBeats is not a function` (and same for `reorderAssetsWithinBeat`).

- [ ] **Step 3: Implement the methods**

In `web/src/app/core/api-client.service.ts`, add to the end of the `ApiClientService` class body, right before the closing `}`:

```ts
  reorderBeats(episodeId: number, orderedIds: number[]): Observable<void> {
    return this.http.patch<void>(`/api/episodes/${episodeId}/beats/reorder`, { orderedIds });
  }

  reorderAssetsWithinBeat(beatId: number, orderedIds: number[]): Observable<void> {
    return this.http.patch<void>(`/api/beats/${beatId}/asset-beats/reorder`, { orderedIds });
  }
```

No new imports needed — `HttpClient` is already injected as `this.http`, and `Observable` is already imported.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `cd web && npx ng test --watch=false`

Expected: PASS, all tests including the two new ones.

- [ ] **Step 5: Commit**

```bash
git add web/src/app/core/api-client.service.ts web/src/app/core/api-client.service.spec.ts
git commit -m "Add reorderBeats and reorderAssetsWithinBeat to ApiClientService"
```

---

## Task 3: Add drag-drop handlers to `BibleComponent`

**Files:**
- Modify: `web/src/app/bible/bible.component.ts`
- Modify: `web/src/app/bible/bible.component.spec.ts`

**Interfaces:**
- Consumes: `ApiClientService.reorderBeats`/`reorderAssetsWithinBeat` from Task 2. `BeatDto.assetIds: number[]` and `BibleComponent.assetsForBeat(beat: BeatDto): AssetDto[]` (both already exist, unchanged).
- Produces: `BibleComponent.onBeatDrop(event: CdkDragDrop<BeatDto[]>): void`, `BibleComponent.onAssetDrop(beat: BeatDto, event: CdkDragDrop<AssetDto[]>): void` — both consumed by Task 4's template.

**Design note on optimistic reordering:** `assetsForBeat(beat)` is a derived method — it recomputes a fresh array from `beat.assetIds` every time it's called, rather than returning a stored array. Reordering a throwaway array returned by one call to `assetsForBeat` would not be visible on the next change-detection pass, since the next call recomputes from the unchanged `beat.assetIds`. So `onAssetDrop` reorders `beat.assetIds` itself (in place) — `assetsForBeat`'s next call then naturally returns the new order — matching how `onBeatDrop` reorders `this.beats` itself for the same reason.

- [ ] **Step 1: Write the failing tests**

Add to `web/src/app/bible/bible.component.spec.ts`, after the existing `'saves inline edits...'` test (before the closing `});` of the outer `describe` block). First extend the `apiSpy` creation in `beforeEach` (existing line 25) to also stub the two new methods:

```ts
    apiSpy = jasmine.createSpyObj('ApiClientService', [
      'getEpisodes', 'getBeats', 'getAssets', 'updateAsset', 'reorderBeats', 'reorderAssetsWithinBeat',
    ]);
```

(This replaces the existing shorter array literal on that line — same call, more method names.)

Then add:

```ts
  it('reorders beats in place and calls reorderBeats with the new order', () => {
    const secondBeat: BeatDto = { id: 101, episodeId: 10, timecode: '00:38', purpose: 'Next', ordinal: 1, durationSeconds: 30, startSeconds: 38, endSeconds: 68, assetIds: [] };
    component.beats = [beat, secondBeat];
    apiSpy.reorderBeats.and.returnValue(of(undefined));

    component.onBeatDrop({ previousIndex: 0, currentIndex: 1 } as any);

    expect(component.beats.map((b) => b.id)).toEqual([101, 100]);
    expect(apiSpy.reorderBeats).toHaveBeenCalledWith(10, [101, 100]);
  });

  it('reorders a beat\'s asset links in place and calls reorderAssetsWithinBeat with the new order', () => {
    const multiBeat: BeatDto = { id: 102, episodeId: 10, timecode: '01:00', purpose: 'Setup', ordinal: 2, durationSeconds: 20, startSeconds: 68, endSeconds: 88, assetIds: [1000, 1001] };
    component.assets = [linkedAsset, unlinkedAsset];
    apiSpy.reorderAssetsWithinBeat.and.returnValue(of(undefined));

    component.onAssetDrop(multiBeat, { previousIndex: 0, currentIndex: 1 } as any);

    expect(multiBeat.assetIds).toEqual([1001, 1000]);
    expect(apiSpy.reorderAssetsWithinBeat).toHaveBeenCalledWith(102, [1001, 1000]);
  });

  it('reloads the episode after a successful beat reorder', () => {
    component.beats = [beat];
    apiSpy.reorderBeats.and.returnValue(of(undefined));
    apiSpy.getBeats.calls.reset();
    apiSpy.getAssets.calls.reset();

    component.onBeatDrop({ previousIndex: 0, currentIndex: 0 } as any);

    expect(apiSpy.getBeats).toHaveBeenCalledWith(10);
    expect(apiSpy.getAssets).toHaveBeenCalledWith(10);
  });
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd web && npx ng test --watch=false`

Expected: FAIL — `component.onBeatDrop is not a function` (and same for `onAssetDrop`).

- [ ] **Step 3: Implement the handlers**

In `web/src/app/bible/bible.component.ts`:

Change the import line that currently reads:

```ts
import { ChangeDetectorRef, Component, effect } from '@angular/core';
```

No change needed to that line. Add a new import line right after it:

```ts
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
```

Change the `@Component` decorator's `imports` array from:

```ts
  imports: [CommonModule, FormsModule, TimelineViewComponent],
```

to:

```ts
  imports: [CommonModule, FormsModule, DragDropModule, TimelineViewComponent],
```

Add these two methods to the `BibleComponent` class, right after `assetsForBeat` (after line 61's closing `}`):

```ts
  onBeatDrop(event: CdkDragDrop<BeatDto[]>): void {
    moveItemInArray(this.beats, event.previousIndex, event.currentIndex);
    const orderedIds = this.beats.map((b) => b.id);
    this.cdr.markForCheck();
    this.api.reorderBeats(this.selectedEpisodeId!, orderedIds).subscribe(() => {
      this.selectEpisode(this.selectedEpisodeId!);
    });
  }

  onAssetDrop(beat: BeatDto, event: CdkDragDrop<AssetDto[]>): void {
    moveItemInArray(beat.assetIds, event.previousIndex, event.currentIndex);
    const orderedIds = [...beat.assetIds];
    this.cdr.markForCheck();
    this.api.reorderAssetsWithinBeat(beat.id, orderedIds).subscribe(() => {
      this.selectEpisode(this.selectedEpisodeId!);
    });
  }
```

Note the `markForCheck()` call right after each optimistic mutation, before the HTTP call — this is what makes the optimistic move actually paint on screen in this zoneless app (see Global Constraints). `selectEpisode` (called in each `.subscribe()`) already calls `markForCheck()` itself once the reload lands (see its existing body), so no second call is needed there.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `cd web && npx ng test --watch=false`

Expected: PASS, all tests including the three new ones.

- [ ] **Step 5: Commit**

```bash
git add web/src/app/bible/bible.component.ts web/src/app/bible/bible.component.spec.ts
git commit -m "Add beat and asset drag-reorder handlers to BibleComponent"
```

---

## Task 4: Wire drag-drop markup into the template, rebuild, and verify end-to-end

**Files:**
- Modify: `web/src/app/bible/bible.component.html`
- Modify: `src/ProductionBible.Api/wwwroot/browser/**` (rebuilt bundle)

**Interfaces:**
- Consumes: `BibleComponent.onBeatDrop`/`onAssetDrop` from Task 3.

- [ ] **Step 1: Replace the beats loop with a `cdkDropList`**

In `web/src/app/bible/bible.component.html`, replace the block from the opening `<ng-container *ngFor="let beat of beats">` (line 26) through its matching `</ng-container>` (line 66) with:

```html
    <div cdkDropList (cdkDropListDropped)="onBeatDrop($event)" class="space-y-4">
      <section *ngFor="let beat of beats" cdkDrag class="rounded-lg border border-border bg-surface p-4">
        <div class="mb-3 flex items-center gap-2">
          <span cdkDragHandle class="cursor-grab select-none text-text-secondary" title="Drag to reorder beat">&#8942;&#8942;</span>
          <h3 class="text-sm font-semibold text-text-secondary">{{ beat.timecode }} &mdash; {{ beat.purpose }}</h3>
        </div>
        <ul cdkDropList (cdkDropListDropped)="onAssetDrop(beat, $event)" class="space-y-2">
          <li *ngFor="let asset of assetsForBeat(beat)" cdkDrag class="rounded-md border border-border bg-bg p-3">
            <div class="flex flex-wrap items-center gap-2">
              <span cdkDragHandle class="cursor-grab select-none text-text-secondary" title="Drag to reorder shot">&#8942;&#8942;</span>
              <span class="font-mono text-sm text-text-secondary">{{ asset.code }}</span>
              <span class="font-medium">{{ asset.title }}</span>
              <span class="text-xs text-text-secondary">({{ asset.assetTypeName }})</span>
              <span class="rounded-full px-2 py-0.5 text-xs font-medium text-white" [ngClass]="statusPillClass(asset.status)">
                {{ asset.status }}
              </span>
              <button type="button" *ngIf="editingAssetId !== asset.id" (click)="startEdit(asset)"
                      class="ml-auto rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Edit</button>
            </div>
            <p *ngIf="asset.notes" class="mt-1 text-sm text-text-secondary">{{ asset.notes }}</p>

            <div *ngIf="editingAssetId === asset.id" class="mt-3 space-y-2 border-t border-border pt-3">
              <label class="block text-sm">
                <span class="mb-1 block text-text-secondary">Status</span>
                <select [(ngModel)]="editStatus"
                        class="w-full rounded-md border border-border bg-surface px-3 py-1.5 text-sm focus:border-accent focus:outline-none">
                  <option *ngFor="let s of assetStatuses" [value]="s">{{ s }}</option>
                </select>
              </label>
              <label class="block text-sm">
                <span class="mb-1 block text-text-secondary">Notes</span>
                <textarea [(ngModel)]="editNotes"
                          class="w-full rounded-md border border-border bg-surface px-3 py-1.5 text-sm focus:border-accent focus:outline-none"></textarea>
              </label>
              <div class="flex gap-2">
                <button type="button" (click)="saveEdit(asset)"
                        class="rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white hover:bg-accent-hover">Save</button>
                <button type="button" (click)="cancelEdit()"
                        class="rounded-md border border-border px-3 py-1.5 text-sm font-medium hover:bg-surface-hover">Cancel</button>
              </div>
            </div>
          </li>
        </ul>
      </section>
    </div>
```

Notes on this change versus the original markup:
- `<ng-container>` (line 26) becomes `<div cdkDropList>` — CDK's `cdkDropList` directive needs a real host element, not a structural-only `<ng-container>`. The new `<div>` carries `class="space-y-4"` to preserve vertical spacing between beat sections now that they're no longer direct children of the outer `<div class="space-y-6">` (line 1) — under `<ng-container>` they were transparent, so `space-y-6` on the ancestor applied directly between `<section>` siblings; now the gap is one level down.
- The beat header (`<h3>`) is wrapped in a new flex row alongside the drag handle span.
- Each `<li cdkDrag>` gets its own drag handle span as the first child of its existing flex row, ahead of the asset code span.
- Everything else inside each `<li>` (asset code/title/type/status pill, Edit button, notes paragraph, inline edit form) is unchanged from the current file.
- `&#8942;&#8942;` renders as two vertical-ellipsis characters (⋮⋮), used as the drag-handle glyph — no icon library needed for one glyph.

The `<ng-template #timelineTpl>` block (lines 108-110) and everything before line 26 are unchanged.

- [ ] **Step 2: Run the component test suite**

Run: `cd web && npx ng test --watch=false`

Expected: PASS — Task 3's tests call the handlers directly and don't depend on the template, but this confirms the template still compiles against the component (Angular's build step type-checks template bindings).

- [ ] **Step 3: Rebuild the frontend into wwwroot**

Run: `cd web && npx ng build --output-path=../src/ProductionBible.Api/wwwroot`

Expected: build succeeds with no errors.

- [ ] **Step 4: Run the app and verify manually**

Run: `dotnet run --project src/ProductionBible.Api`

Open `http://localhost:5280`, navigate to Storyboard, select an episode with at least two beats and a beat with at least two shots. Verify:
- Each beat section and each shot row shows a `⋮⋮` drag handle.
- Dragging a beat by its handle to a new position reorders it, and the new order persists across switching to another episode and back (confirms `reorderBeats` round-tripped and the server accepted it).
- Dragging a shot by its handle to a new position within its own beat reorders it, and the new order persists the same way (confirms `reorderAssetsWithinBeat` round-tripped).
- Dragging a shot does not offer to drop it into a different beat's list (the two shot lists are independent `cdkDropList`s).
- The Timeline view toggle still renders unchanged (unaffected by this change).
- Clicking "Edit" on a beat's shot (not its drag handle) still opens the inline editor — confirms the drag handle didn't swallow the Edit button's click target.

If any of these fail, fix before proceeding — this is the step that has caught real, otherwise-invisible bugs in this project before (see Global Constraints).

- [ ] **Step 5: Commit the rebuilt wwwroot alongside the template change**

```bash
git add web/src/app/bible/bible.component.html src/ProductionBible.Api/wwwroot
git commit -m "Wire CDK drag-drop into the Storyboard list view for beat and shot reorder"
```

---

## Self-Review Notes

- **Spec coverage:** Task 1 covers the new dependency. Task 2 covers the API client additions. Task 3 covers the component handlers, including the optimistic-mutation design note the spec's illustrative code didn't fully work out (mutating `beat.assetIds` directly rather than a throwaway array from `assetsForBeat()` — otherwise the shot reorder wouldn't visibly move until the reconciling reload landed, silently breaking the spec's own "CDK moves the DOM item immediately" claim for shots specifically). Task 4 covers the template wiring, the wwwroot rebuild gotcha, and manual verification. Every spec section (Context/scope, CDK dependency, interaction design, component structure, API client additions, error handling, testing, deferred items) maps to a task above.
- **Placeholder scan:** no TBD/TODO; every step has literal code or an exact command.
- **Type consistency:** `onBeatDrop(event: CdkDragDrop<BeatDto[]>)` / `onAssetDrop(beat: BeatDto, event: CdkDragDrop<AssetDto[]>)` match between Task 3's implementation and its tests (tests cast a plain object `as any` to `CdkDragDrop<...>` rather than constructing a real CDK event, since only `previousIndex`/`currentIndex` are read — consistent with the spec's Testing section: "No test exercises CDK's actual pointer/drag mechanics").
- **Scope:** four tasks, each independently testable/committable, none touching backend code. Matches the spec's frontend-only, single-view scope.
