# Asset View Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract Manage > Assets' inline edit form into a reusable `AssetEditorComponent`, then build a new sortable/filterable Asset view on top of it — the final Track B sub-project.

**Architecture:** Task 1 is a behavior-preserving refactor: move `AssetsTabComponent`'s edit-form fields/methods into a standalone `AssetEditorComponent` (`@Input`s for the asset + its lookup lists, `@Output`s for saved/cancelled), which becomes the shared editor both Manage > Assets and the new Asset view use. Tasks 2-4 build the new view: pure sort/filter logic, the component (data loading, mirroring `ProductionPlanComponent`'s per-episode `forkJoin` pattern), then the template (table + a new modal-overlay pattern, since this app has none yet) plus route/nav wiring.

**Tech Stack:** Angular 22 (zoneless), RxJS, Jasmine/Karma. No backend changes.

**Spec:** `docs/superpowers/specs/2026-09-12-asset-view-design.md`

## Global Constraints

- No backend changes. Every field/endpoint this sub-project uses already exists.
- `AssetEditorComponent` owns its own `ApiClientService.updateAsset()` call and emits the updated `AssetDto` on success via `(saved)` — callers never call `updateAsset` themselves.
- No delete, no create, from the new Asset view — only Manage > Assets keeps those.
- Sort/filter logic lives in a plain, dependency-free file (`asset-view.logic.ts`), not inline in the component — mirrors this repo's existing `phase-grouping.ts`/`timeline-model.ts` pattern.
- Numeric sort-key fallbacks use `Number.MAX_SAFE_INTEGER` for null/missing values, consistent with this repo's existing `?? Number.MAX_SAFE_INTEGER` / `?? int.MaxValue` conventions elsewhere.
- The app is zoneless (no `provideZoneChangeDetection`) — every state mutation from a `.subscribe()` callback or event handler that should repaint needs an explicit `this.cdr.markForCheck()` call.
- Any change under `web/src/` needs the wwwroot rebuild (`cd web && npx ng build --output-path=../src/ProductionBible.Api/wwwroot`) as its own explicit step, verified by actually running `dotnet run` — this is Task 4, not optional polish.

---

## File Structure

- Create: `web/src/app/manage/asset-editor.component.ts`, `.html`, `.spec.ts` — the extracted, reusable editor.
- Modify: `web/src/app/manage/assets-tab.component.ts`, `.html`, `.spec.ts` — shrinks to use `<app-asset-editor>`.
- Create: `web/src/app/asset-view/asset-view.logic.ts`, `.spec.ts` — pure sort/filter functions.
- Create: `web/src/app/asset-view/asset-view.component.ts`, `.html`, `.spec.ts` — the new view.
- Modify: `web/src/app/app.routes.ts` — new `/assets` route.
- Modify: `web/src/app/app.html` — new "Assets" nav link, desktop + mobile.
- Modify: `src/ProductionBible.Api/wwwroot/browser/**` — rebuilt frontend bundle (Task 4).

---

## Task 1: Extract `AssetEditorComponent` out of `AssetsTabComponent`

**Files:**
- Create: `web/src/app/manage/asset-editor.component.ts`
- Create: `web/src/app/manage/asset-editor.component.html`
- Create: `web/src/app/manage/asset-editor.component.spec.ts`
- Modify: `web/src/app/manage/assets-tab.component.ts`
- Modify: `web/src/app/manage/assets-tab.component.html`
- Modify: `web/src/app/manage/assets-tab.component.spec.ts`

**Interfaces:**
- Produces: `AssetEditorComponent` with `@Input({ required: true }) asset!: AssetDto`, `@Input() assetTypes: AssetTypeDto[] = []`, `@Input() phases: PhaseDto[] = []`, `@Input() beats: BeatDto[] = []`, `@Output() saved = new EventEmitter<AssetDto>()`, `@Output() cancelled = new EventEmitter<void>()` — consumed by Task 3/4's `AssetViewComponent` and this task's own rewritten `AssetsTabComponent`.

This task is a refactor of shipped, tested code — the goal is identical external behavior for Manage > Assets, verified by its existing tests (moved/adapted, not weakened), plus a new reusable component.

- [ ] **Step 1: Write `AssetEditorComponent`**

Create `web/src/app/manage/asset-editor.component.ts`:

```ts
import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ASSET_STATUSES } from '../core/status-style';
import { AssetDto, AssetTypeDto, BeatDto, PhaseDto } from '../core/models';

interface AttributeRow {
  key: string;
  value: string;
}

@Component({
  selector: 'app-asset-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './asset-editor.component.html',
})
export class AssetEditorComponent implements OnChanges {
  @Input({ required: true }) asset!: AssetDto;
  @Input() assetTypes: AssetTypeDto[] = [];
  @Input() phases: PhaseDto[] = [];
  @Input() beats: BeatDto[] = [];
  @Output() saved = new EventEmitter<AssetDto>();
  @Output() cancelled = new EventEmitter<void>();

  protected readonly assetStatuses = ASSET_STATUSES;

  editCode = '';
  editTitle = '';
  editScriptText = '';
  editAssetTypeId = 0;
  editStatus = '';
  editNotes = '';
  editSequenceNumber: number | null = null;
  editTargetLengthSeconds: number | null = null;
  editPhaseId: number | null = null;
  editAttributes: AttributeRow[] = [];
  editBeatIds: number[] = [];

  constructor(private readonly api: ApiClientService, private readonly cdr: ChangeDetectorRef) {}

  ngOnChanges(): void {
    this.editCode = this.asset.code;
    this.editTitle = this.asset.title;
    this.editScriptText = this.asset.scriptText ?? '';
    this.editAssetTypeId = this.asset.assetTypeId;
    this.editStatus = this.asset.status;
    this.editNotes = this.asset.notes ?? '';
    this.editSequenceNumber = this.asset.sequenceNumber;
    this.editTargetLengthSeconds = this.asset.targetLengthSeconds;
    this.editPhaseId = this.asset.phaseId;
    this.editAttributes = Object.entries(this.asset.attributes).map(([key, value]) => ({ key, value }));
    this.editBeatIds = [...this.asset.beatIds];
  }

  addAttributeRow(): void {
    this.editAttributes = [...this.editAttributes, { key: '', value: '' }];
  }

  removeAttributeRow(index: number): void {
    this.editAttributes = this.editAttributes.filter((_, i) => i !== index);
  }

  toggleBeatLink(beatId: number, linked: boolean): void {
    this.editBeatIds = linked
      ? [...this.editBeatIds, beatId]
      : this.editBeatIds.filter((id) => id !== beatId);
  }

  save(): void {
    const attributes: Record<string, string> = {};
    for (const row of this.editAttributes) {
      const key = row.key.trim();
      if (key) attributes[key] = row.value;
    }

    this.api.updateAsset(this.asset.id, {
      assetTypeId: this.editAssetTypeId,
      code: this.editCode,
      title: this.editTitle,
      scriptText: this.editScriptText || null,
      status: this.editStatus,
      notes: this.editNotes || null,
      sequenceNumber: this.editSequenceNumber,
      targetLengthSeconds: this.editTargetLengthSeconds,
      phaseId: this.editPhaseId,
      attributes,
      beatIds: this.editBeatIds,
    }).subscribe((updated) => {
      this.saved.emit(updated);
      this.cdr.markForCheck();
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }
}
```

Note the switch from `AssetsTabComponent`'s old pattern (populate edit fields imperatively inside `startEdit(asset)`, called once when the user clicks Edit) to `ngOnChanges` (populate fields whenever the `[asset]` input changes) — this is required because the component is now instantiated once and re-bound to different assets (Asset View's modal reuses one `<app-asset-editor>` across whichever asset is currently selected), not created fresh per edit the way inline `*ngIf="editingId === asset.id"` used to imply.

Create `web/src/app/manage/asset-editor.component.html` — copy the `<ng-template #editForm>` block's *inner content* from the current `web/src/app/manage/assets-tab.component.html` (the `<div class="grid ...">` field grid, the Attributes section, the Beat links section, and the Save/Cancel buttons), with two changes:
1. Drop the `<ng-template #editForm>` wrapper itself — this file's root is just the content that was inside it.
2. Change the Save/Cancel buttons' handlers from `(click)="saveEdit(asset)"` / `(click)="cancelEdit()"` to `(click)="save()"` / `(click)="cancel()"`.

Everything else (all field bindings — `[(ngModel)]="editCode"` etc. — stay identical, since the field names are unchanged) copies verbatim.

- [ ] **Step 2: Write the editor's tests**

Create `web/src/app/manage/asset-editor.component.spec.ts`:

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AssetEditorComponent } from './asset-editor.component';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto, BeatDto } from '../core/models';

describe('AssetEditorComponent', () => {
  let fixture: ComponentFixture<AssetEditorComponent>;
  let component: AssetEditorComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const beat: BeatDto = {
    id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open',
    ordinal: 0, durationSeconds: 38, startSeconds: 0, endSeconds: 38, assetIds: [1000],
  };
  const asset: AssetDto = {
    id: 1000, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01',
    title: 'Tool entering the work', scriptText: null, status: 'Planned', notes: null,
    sequenceNumber: 1, targetLengthSeconds: null, phaseId: 5, completedAtUtc: null,
    attributes: { Location: 'Workshop' }, beatIds: [100], orderInPhase: null,
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['updateAsset']);

    await TestBed.configureTestingModule({
      imports: [AssetEditorComponent],
      providers: [{ provide: ApiClientService, useValue: apiSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetEditorComponent);
    component = fixture.componentInstance;
    component.asset = asset;
    component.beats = [beat];
    fixture.detectChanges();
  });

  it('populates the full field set including attributes and beat links from the asset input', () => {
    expect(component.editCode).toBe('A-01');
    expect(component.editAttributes).toEqual([{ key: 'Location', value: 'Workshop' }]);
    expect(component.editBeatIds).toEqual([100]);
  });

  it('re-populates when the asset input changes', () => {
    const otherAsset: AssetDto = { ...asset, id: 1001, code: 'A-02', attributes: {} };
    component.asset = otherAsset;
    component.ngOnChanges();
    expect(component.editCode).toBe('A-02');
    expect(component.editAttributes).toEqual([]);
  });

  it('saves, rebuilding the attributes map from the editable rows, and emits the updated asset', () => {
    const updated = { ...asset, status: 'Shot' };
    apiSpy.updateAsset.and.returnValue(of(updated));
    component.editAttributes = [{ key: 'Location', value: 'Studio' }, { key: 'Notes', value: 'Reshoot' }];
    component.editBeatIds = [100];
    let emitted: AssetDto | undefined;
    component.saved.subscribe((a) => (emitted = a));

    component.save();

    expect(apiSpy.updateAsset).toHaveBeenCalledWith(1000, jasmine.objectContaining({
      attributes: { Location: 'Studio', Notes: 'Reshoot' },
      beatIds: [100],
    }));
    expect(emitted).toEqual(updated);
  });

  it('emits cancelled on cancel', () => {
    let cancelled = false;
    component.cancelled.subscribe(() => (cancelled = true));
    component.cancel();
    expect(cancelled).toBeTrue();
  });
});
```

- [ ] **Step 3: Run the new tests to verify they fail**

Run: `cd web && npx ng test --watch=false`

Expected: FAIL — `AssetEditorComponent` doesn't exist yet.

- [ ] **Step 4: Confirm the component/template exist and compile (from Steps 1-2)**

Run: `cd web && npx ng test --watch=false`

Expected: the 4 new `AssetEditorComponent` tests pass. `AssetsTabComponent`'s own tests will now be failing/stale (Step 5 fixes this) — confirm the only failures are inside `AssetsTabComponent`'s spec.

- [ ] **Step 5: Rewrite `AssetsTabComponent` to use the editor**

In `web/src/app/manage/assets-tab.component.ts`:
- Remove: `editCode`, `editTitle`, `editScriptText`, `editAssetTypeId`, `editStatus`, `editNotes`, `editSequenceNumber`, `editTargetLengthSeconds`, `editPhaseId`, `editAttributes`, `editBeatIds`, `addAttributeRow`, `removeAttributeRow`, `toggleBeatLink`, `saveEdit`, the `AttributeRow` interface, and the `ASSET_STATUSES`/`assetStatuses` import and field (no longer needed here — the editor owns status options now).
- Keep: `editingId`, `startEdit(asset)` (simplify to just `this.editingId = asset.id;`), `cancelEdit()` (unchanged: `this.editingId = null;`).
- Add:

```ts
  onEditorSaved(updated: AssetDto): void {
    const index = this.assets.findIndex((a) => a.id === updated.id);
    if (index !== -1) {
      this.assets[index] = updated;
    }
    this.editingId = null;
    this.cdr.markForCheck();
  }
```

- Update the `imports` array in the `@Component` decorator: add `AssetEditorComponent` (import from `./asset-editor.component`), remove nothing else needed by the remaining template.

In `web/src/app/manage/assets-tab.component.html`, replace the entire `<ng-template #editForm> ... </ng-template>` block with:

```html
      <ng-template #editForm>
        <app-asset-editor [asset]="asset" [assetTypes]="assetTypes" [phases]="phases" [beats]="beats"
                          (saved)="onEditorSaved($event)" (cancelled)="cancelEdit()"></app-asset-editor>
      </ng-template>
```

(The `<ng-container *ngIf="editingId !== asset.id; else editForm">`/row-summary markup above it, and everything else in the file, stays unchanged.)

- [ ] **Step 6: Update `AssetsTabComponent`'s tests**

In `web/src/app/manage/assets-tab.component.spec.ts`, remove the two tests that now belong to the editor
("starts an edit with the full field set including attributes and beat links", "saves an edit, rebuilding
the attributes map from the editable rows") — they're covered by Step 2's new `asset-editor.component.spec.ts`.
Add:

```ts
  it('replaces the row and clears editingId when the editor emits saved', () => {
    component.startEdit(asset);
    const updated = { ...asset, status: 'Shot' };

    component.onEditorSaved(updated);

    expect(component.assets.find((a) => a.id === asset.id)?.status).toBe('Shot');
    expect(component.editingId).toBeNull();
  });
```

`apiSpy`'s `createAsset`/`updateAsset`/`deleteAsset` stub list can drop `updateAsset` if nothing else in this
spec file calls it after this change — check before removing; leave it in the `createSpyObj` list if in doubt,
an unused stub is harmless.

- [ ] **Step 7: Run the full frontend test suite**

Run: `cd web && npx ng test --watch=false`

Expected: all tests pass — `AssetEditorComponent`'s 4 tests, `AssetsTabComponent`'s remaining + 1 new test, and everything else in the suite unaffected.

- [ ] **Step 8: Commit**

```bash
git add web/src/app/manage/asset-editor.component.ts web/src/app/manage/asset-editor.component.html \
        web/src/app/manage/asset-editor.component.spec.ts web/src/app/manage/assets-tab.component.ts \
        web/src/app/manage/assets-tab.component.html web/src/app/manage/assets-tab.component.spec.ts
git commit -m "Extract AssetEditorComponent out of AssetsTabComponent

Behavior-preserving refactor: Manage > Assets' inline edit form
becomes a reusable, self-contained component (owns its own
updateAsset call, emits the result) so the upcoming Asset view can
reuse the exact same editor rather than reimplementing it.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 2: `asset-view.logic.ts` — pure sort/filter functions

**Files:**
- Create: `web/src/app/asset-view/asset-view.logic.ts`
- Create: `web/src/app/asset-view/asset-view.logic.spec.ts`

**Interfaces:**
- Produces: `SortKey`, `SortDir`, `AssetViewFilters`, `filterAndSortAssets(assets, assetTypesById, beatsById, filters, sortKey, sortDir): AssetDto[]` — consumed by Task 3's `AssetViewComponent`.

- [ ] **Step 1: Write the failing tests**

Create `web/src/app/asset-view/asset-view.logic.spec.ts`:

```ts
import { AssetDto, AssetTypeDto, BeatDto } from '../core/models';
import { filterAndSortAssets } from './asset-view.logic';

function asset(overrides: Partial<AssetDto>): AssetDto {
  return {
    id: 1, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01', title: 'Alpha',
    scriptText: null, status: 'Planned', notes: null, sequenceNumber: null, targetLengthSeconds: null,
    phaseId: null, completedAtUtc: null, attributes: {}, beatIds: [], orderInPhase: null,
    ...overrides,
  };
}

const shotType: AssetTypeDto = { id: 1, name: 'Shot' };
const graphicType: AssetTypeDto = { id: 2, name: 'Graphic' };
const assetTypesById = new Map([[1, shotType], [2, graphicType]]);

describe('filterAndSortAssets', () => {
  it('sorts by type name', () => {
    const a = asset({ id: 1, assetTypeId: 2 });
    const b = asset({ id: 2, assetTypeId: 1 });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'type', 'asc');
    expect(result.map((x) => x.id)).toEqual([2, 1]); // 'Graphic' > 'Shot', so Shot (b) sorts first ascending
  });

  it('sorts by name (title)', () => {
    const a = asset({ id: 1, title: 'Zebra' });
    const b = asset({ id: 2, title: 'Apple' });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'name', 'asc');
    expect(result.map((x) => x.id)).toEqual([2, 1]);
  });

  it('sorts by length, nulls last ascending', () => {
    const a = asset({ id: 1, targetLengthSeconds: null });
    const b = asset({ id: 2, targetLengthSeconds: 5 });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'length', 'asc');
    expect(result.map((x) => x.id)).toEqual([2, 1]);
  });

  it('sorts by timeline order using the minimum linked beat ordinal, unlinked last', () => {
    const beatsById = new Map<number, BeatDto>([
      [100, { id: 100, episodeId: 10, timecode: '00:00', purpose: 'a', ordinal: 5, durationSeconds: 1, startSeconds: 0, endSeconds: 1, assetIds: [] }],
      [101, { id: 101, episodeId: 10, timecode: '00:01', purpose: 'b', ordinal: 1, durationSeconds: 1, startSeconds: 1, endSeconds: 2, assetIds: [] }],
    ]);
    const a = asset({ id: 1, beatIds: [100] }); // ordinal 5
    const b = asset({ id: 2, beatIds: [101] }); // ordinal 1
    const c = asset({ id: 3, beatIds: [] });     // unlinked
    const result = filterAndSortAssets([a, b, c], assetTypesById, beatsById, { status: null, assetTypeId: null, toShootOnly: false }, 'timeline', 'asc');
    expect(result.map((x) => x.id)).toEqual([2, 1, 3]);
  });

  it('sorts by shooting order using sequenceNumber, nulls last ascending', () => {
    const a = asset({ id: 1, sequenceNumber: null });
    const b = asset({ id: 2, sequenceNumber: 3 });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'shooting', 'asc');
    expect(result.map((x) => x.id)).toEqual([2, 1]);
  });

  it('reverses order when sortDir is desc', () => {
    const a = asset({ id: 1, title: 'Apple' });
    const b = asset({ id: 2, title: 'Zebra' });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'name', 'desc');
    expect(result.map((x) => x.id)).toEqual([2, 1]);
  });

  it('filters by status', () => {
    const a = asset({ id: 1, status: 'Planned' });
    const b = asset({ id: 2, status: 'Shot' });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: 'Shot', assetTypeId: null, toShootOnly: false }, 'name', 'asc');
    expect(result.map((x) => x.id)).toEqual([2]);
  });

  it('filters by assetTypeId', () => {
    const a = asset({ id: 1, assetTypeId: 1 });
    const b = asset({ id: 2, assetTypeId: 2 });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: 2, toShootOnly: false }, 'name', 'asc');
    expect(result.map((x) => x.id)).toEqual([2]);
  });

  it('filters to only assets with no completedAtUtc when toShootOnly is true', () => {
    const a = asset({ id: 1, completedAtUtc: '2026-09-01T00:00:00Z' });
    const b = asset({ id: 2, completedAtUtc: null });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: true }, 'name', 'asc');
    expect(result.map((x) => x.id)).toEqual([2]);
  });
});
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd web && npx ng test --watch=false`

Expected: FAIL — `asset-view.logic` module doesn't exist.

- [ ] **Step 3: Implement `asset-view.logic.ts`**

Create `web/src/app/asset-view/asset-view.logic.ts`:

```ts
import { AssetDto, AssetTypeDto, BeatDto } from '../core/models';

export type SortKey = 'type' | 'name' | 'length' | 'timeline' | 'shooting';
export type SortDir = 'asc' | 'desc';

export interface AssetViewFilters {
  status: string | null;
  assetTypeId: number | null;
  toShootOnly: boolean;
}

function timelineOrderOf(asset: AssetDto, beatsById: Map<number, BeatDto>): number {
  const ordinals = asset.beatIds
    .map((id) => beatsById.get(id)?.ordinal)
    .filter((o): o is number => o !== undefined);
  return ordinals.length > 0 ? Math.min(...ordinals) : Number.MAX_SAFE_INTEGER;
}

function sortKeyValue(
  asset: AssetDto,
  assetTypesById: Map<number, AssetTypeDto>,
  beatsById: Map<number, BeatDto>,
  key: SortKey,
): string | number {
  switch (key) {
    case 'type':
      return assetTypesById.get(asset.assetTypeId)?.name ?? '';
    case 'name':
      return asset.title;
    case 'length':
      return asset.targetLengthSeconds ?? Number.MAX_SAFE_INTEGER;
    case 'timeline':
      return timelineOrderOf(asset, beatsById);
    case 'shooting':
      return asset.sequenceNumber ?? Number.MAX_SAFE_INTEGER;
  }
}

export function filterAndSortAssets(
  assets: AssetDto[],
  assetTypesById: Map<number, AssetTypeDto>,
  beatsById: Map<number, BeatDto>,
  filters: AssetViewFilters,
  sortKey: SortKey,
  sortDir: SortDir,
): AssetDto[] {
  const filtered = assets.filter((asset) => {
    if (filters.status !== null && asset.status !== filters.status) return false;
    if (filters.assetTypeId !== null && asset.assetTypeId !== filters.assetTypeId) return false;
    if (filters.toShootOnly && asset.completedAtUtc !== null) return false;
    return true;
  });

  const sorted = [...filtered].sort((a, b) => {
    const av = sortKeyValue(a, assetTypesById, beatsById, sortKey);
    const bv = sortKeyValue(b, assetTypesById, beatsById, sortKey);
    if (av < bv) return -1;
    if (av > bv) return 1;
    return 0;
  });

  return sortDir === 'asc' ? sorted : sorted.reverse();
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `cd web && npx ng test --watch=false`

Expected: PASS, all 9 new tests.

- [ ] **Step 5: Commit**

```bash
git add web/src/app/asset-view/asset-view.logic.ts web/src/app/asset-view/asset-view.logic.spec.ts
git commit -m "Add pure sort/filter logic for the upcoming Asset view

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 3: `AssetViewComponent` — data loading and state

**Files:**
- Create: `web/src/app/asset-view/asset-view.component.ts`
- Create: `web/src/app/asset-view/asset-view.component.spec.ts`

**Interfaces:**
- Consumes: `filterAndSortAssets`/`SortKey`/`SortDir`/`AssetViewFilters` (Task 2), `AssetEditorComponent` (Task 1), `ApiClientService.getEpisodes`/`getAssets`/`getBeats`/`getAssetTypes`/`getPhases` (all pre-existing).
- Produces: `AssetViewComponent` with `visibleAssets` (getter or computed field), `sortKey`/`sortDir`/`filters` state, `setSortKey(key)`, filter-changing methods, `selectedAssetForEdit`, `openEditor(asset)`, `onEditorSaved(updated)`, `closeEditor()` — consumed by Task 4's template.

- [ ] **Step 1: Write the failing tests**

Create `web/src/app/asset-view/asset-view.component.spec.ts`:

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { AssetViewComponent } from './asset-view.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, AssetTypeDto, BeatDto, EpisodeDto, PhaseDto, ProjectDto } from '../core/models';

function asset(overrides: Partial<AssetDto>): AssetDto {
  return {
    id: 1, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01', title: 'Alpha',
    scriptText: null, status: 'Planned', notes: null, sequenceNumber: null, targetLengthSeconds: null,
    phaseId: null, completedAtUtc: null, attributes: {}, beatIds: [], orderInPhase: null,
    ...overrides,
  };
}

describe('AssetViewComponent', () => {
  let fixture: ComponentFixture<AssetViewComponent>;
  let component: AssetViewComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode1: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const episode2: EpisodeDto = { id: 11, projectId: 1, name: 'EP2', orderIndex: 2 };
  const assetType: AssetTypeDto = { id: 1, name: 'Shot' };
  const phase: PhaseDto = { id: 5, projectId: 1, name: 'Setup A', orderIndex: 0 };
  const beat: BeatDto = {
    id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open',
    ordinal: 0, durationSeconds: 38, startSeconds: 0, endSeconds: 38, assetIds: [1000],
  };
  const assetA = asset({ id: 1000, episodeId: 10, code: 'A-01', title: 'Alpha', beatIds: [100] });
  const assetB = asset({ id: 1001, episodeId: 11, code: 'B-01', title: 'Beta' });

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', [
      'getEpisodes', 'getAssets', 'getBeats', 'getAssetTypes', 'getPhases',
    ]);
    apiSpy.getEpisodes.and.returnValue(of([episode1, episode2]));
    apiSpy.getAssetTypes.and.returnValue(of([assetType]));
    apiSpy.getPhases.and.returnValue(of([phase]));
    apiSpy.getAssets.and.callFake((episodeId: number) =>
      of(episodeId === episode1.id ? [assetA] : [assetB]));
    apiSpy.getBeats.and.callFake((episodeId: number) =>
      of(episodeId === episode1.id ? [beat] : []));

    const projectContextStub = {
      projects: signal<ProjectDto[]>([project]),
      selectedProjectId: signal<number | null>(project.id),
      selectProject: jasmine.createSpy('selectProject'),
    };

    await TestBed.configureTestingModule({
      imports: [AssetViewComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: projectContextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    fixture.detectChanges();
  });

  it('merges assets from every episode in the project', () => {
    expect(component.visibleAssets.map((a) => a.id).sort()).toEqual([1000, 1001]);
  });

  it('opens the editor with the clicked asset and its own episode\'s beats', () => {
    component.openEditor(assetA);
    expect(component.selectedAssetForEdit).toEqual(assetA);
    expect(component.beatsByEpisode.get(10)).toEqual([beat]);
  });

  it('updates the row and closes the editor when saved', () => {
    component.openEditor(assetA);
    const updated = { ...assetA, status: 'Shot' };

    component.onEditorSaved(updated);

    expect(component.assets.find((a) => a.id === assetA.id)?.status).toBe('Shot');
    expect(component.selectedAssetForEdit).toBeNull();
  });

  it('closes the editor without saving on closeEditor', () => {
    component.openEditor(assetA);
    component.closeEditor();
    expect(component.selectedAssetForEdit).toBeNull();
  });

  it('toggles sort direction when the same key is set twice', () => {
    component.setSortKey('name');
    expect(component.sortDir).toBe('asc');
    component.setSortKey('name');
    expect(component.sortDir).toBe('desc');
    component.setSortKey('length');
    expect(component.sortDir).toBe('asc');
  });
});
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd web && npx ng test --watch=false`

Expected: FAIL — `AssetViewComponent` doesn't exist.

- [ ] **Step 3: Implement `AssetViewComponent`**

Create `web/src/app/asset-view/asset-view.component.ts`:

```ts
import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { ASSET_STATUSES, statusPillClass } from '../core/status-style';
import { AssetDto, AssetTypeDto, BeatDto, PhaseDto } from '../core/models';
import { AssetEditorComponent } from '../manage/asset-editor.component';
import { AssetViewFilters, filterAndSortAssets, SortDir, SortKey } from './asset-view.logic';

@Component({
  selector: 'app-asset-view',
  standalone: true,
  imports: [CommonModule, FormsModule, AssetEditorComponent],
  templateUrl: './asset-view.component.html',
})
export class AssetViewComponent {
  assets: AssetDto[] = [];
  assetTypes: AssetTypeDto[] = [];
  phases: PhaseDto[] = [];
  assetTypesById = new Map<number, AssetTypeDto>();
  beatsById = new Map<number, BeatDto>();
  beatsByEpisode = new Map<number, BeatDto[]>();

  sortKey: SortKey = 'shooting';
  sortDir: SortDir = 'asc';
  filters: AssetViewFilters = { status: null, assetTypeId: null, toShootOnly: false };

  selectedAssetForEdit: AssetDto | null = null;

  protected readonly assetStatuses = ASSET_STATUSES;
  protected readonly statusPillClass = statusPillClass;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    this.api.getAssetTypes().subscribe((types) => {
      this.assetTypes = types;
      this.assetTypesById = new Map(types.map((t) => [t.id, t]));
      this.cdr.markForCheck();
    });

    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      if (projectId !== null) {
        this.loadData(projectId);
      }
    });
  }

  get visibleAssets(): AssetDto[] {
    return filterAndSortAssets(this.assets, this.assetTypesById, this.beatsById, this.filters, this.sortKey, this.sortDir);
  }

  setSortKey(key: SortKey): void {
    this.sortDir = this.sortKey === key && this.sortDir === 'asc' ? 'desc' : 'asc';
    this.sortKey = key;
  }

  openEditor(asset: AssetDto): void {
    this.selectedAssetForEdit = asset;
  }

  closeEditor(): void {
    this.selectedAssetForEdit = null;
  }

  onEditorSaved(updated: AssetDto): void {
    const index = this.assets.findIndex((a) => a.id === updated.id);
    if (index !== -1) {
      this.assets[index] = updated;
    }
    this.selectedAssetForEdit = null;
    this.cdr.markForCheck();
  }

  private loadData(projectId: number): void {
    forkJoin({
      episodes: this.api.getEpisodes(projectId),
      phases: this.api.getPhases(projectId),
    }).subscribe(({ episodes, phases }) => {
      if (this.projectContext.selectedProjectId() !== projectId) return;
      this.phases = phases;
      if (episodes.length === 0) {
        this.assets = [];
        this.beatsById = new Map();
        this.beatsByEpisode = new Map();
        this.cdr.markForCheck();
        return;
      }

      forkJoin(episodes.map((episode) => forkJoin({
        episodeId: Promise.resolve(episode.id) as any,
        assets: this.api.getAssets(episode.id),
        beats: this.api.getBeats(episode.id),
      }))).subscribe((perEpisode) => {
        if (this.projectContext.selectedProjectId() !== projectId) return;
        this.assets = perEpisode.flatMap((e) => e.assets);
        this.beatsByEpisode = new Map(perEpisode.map((e) => [e.episodeId, e.beats]));
        this.beatsById = new Map(perEpisode.flatMap((e) => e.beats).map((b) => [b.id, b]));
        this.cdr.markForCheck();
      });
      this.cdr.markForCheck();
    });
  }
}
```

Note: the `Promise.resolve(episode.id) as any` inside the inner `forkJoin({...})` is a placeholder to smuggle
`episode.id` through alongside the two real HTTP observables — **do not actually write this**; it's flagged
here as a wrong approach so you don't reach for it. Instead, use RxJS's `map` to attach the episode id after
the inner `forkJoin` resolves:

```ts
      forkJoin(episodes.map((episode) =>
        forkJoin({ assets: this.api.getAssets(episode.id), beats: this.api.getBeats(episode.id) })
          .pipe(map((result) => ({ episodeId: episode.id, ...result }))),
      )).subscribe((perEpisode) => {
```

Add `import { forkJoin, map } from 'rxjs';` (replacing the plain `import { forkJoin } from 'rxjs';`) at the
top of the file to support this. Use this version, not the `Promise.resolve` placeholder shown above.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `cd web && npx ng test --watch=false`

Expected: PASS — all 5 new `AssetViewComponent` tests, plus everything from Tasks 1-2 still green. (The
template file doesn't exist yet — Angular's test runner needs `templateUrl` to resolve to *some* file to
compile the component; create an empty placeholder `web/src/app/asset-view/asset-view.component.html`
containing just `<div></div>` for this step if the build fails without one. Task 4 replaces it.)

- [ ] **Step 5: Commit**

```bash
git add web/src/app/asset-view/asset-view.component.ts web/src/app/asset-view/asset-view.component.spec.ts \
        web/src/app/asset-view/asset-view.component.html
git commit -m "Add AssetViewComponent data loading and editor-modal state

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 4: Template, route/nav wiring, rebuild, and verify

**Files:**
- Modify: `web/src/app/asset-view/asset-view.component.html` (replace the Task 3 placeholder)
- Modify: `web/src/app/app.routes.ts`
- Modify: `web/src/app/app.html`
- Modify: `src/ProductionBible.Api/wwwroot/browser/**` (rebuilt bundle)

**Interfaces:**
- Consumes: `AssetViewComponent`'s public members (Task 3).

- [ ] **Step 1: Wire the route and nav**

In `web/src/app/app.routes.ts`, add the import and route:

```ts
import { AssetViewComponent } from './asset-view/asset-view.component';
```

```ts
  { path: 'assets', component: AssetViewComponent },
```

(Add this line after the existing `{ path: 'manage', component: ManageComponent }` line.)

In `web/src/app/app.html`, add a new nav link in both the desktop `<nav>` (after the "Manage" link) and the
mobile `<nav>` (same position):

```html
          <a routerLink="/assets" routerLinkActive="bg-accent text-white"
             class="rounded-md px-3 py-1.5 text-sm font-medium text-text-secondary hover:bg-surface-hover hover:text-text-primary">Assets</a>
```

```html
      <a routerLink="/assets" (click)="toggleMobileNav()"
         class="rounded-md px-3 py-2 text-sm font-medium text-text-secondary hover:bg-surface-hover">Assets</a>
```

- [ ] **Step 2: Write the template**

Replace `web/src/app/asset-view/asset-view.component.html`'s placeholder content with:

```html
<div class="space-y-4">
  <div class="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-surface p-4">
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Status</span>
      <select [(ngModel)]="filters.status"
              class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none">
        <option [ngValue]="null">All</option>
        <option *ngFor="let s of assetStatuses" [ngValue]="s">{{ s }}</option>
      </select>
    </label>
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Shot type</span>
      <select [(ngModel)]="filters.assetTypeId"
              class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none">
        <option [ngValue]="null">All</option>
        <option *ngFor="let t of assetTypes" [ngValue]="t.id">{{ t.name }}</option>
      </select>
    </label>
    <label class="flex items-center gap-1.5 text-sm">
      <input type="checkbox" [(ngModel)]="filters.toShootOnly" />
      To shoot only
    </label>
  </div>

  <div class="overflow-x-auto rounded-lg border border-border bg-surface">
    <table class="w-full text-left text-sm">
      <thead>
        <tr class="border-b border-border text-xs uppercase tracking-wide text-text-secondary">
          <th class="px-4 py-2">Code</th>
          <th class="cursor-pointer px-4 py-2 select-none" (click)="setSortKey('name')">Title</th>
          <th class="cursor-pointer px-4 py-2 select-none" (click)="setSortKey('type')">Type</th>
          <th class="cursor-pointer px-4 py-2 select-none" (click)="setSortKey('length')">Length</th>
          <th class="cursor-pointer px-4 py-2 select-none" (click)="setSortKey('timeline')">Timeline order</th>
          <th class="cursor-pointer px-4 py-2 select-none" (click)="setSortKey('shooting')">Shooting order</th>
          <th class="px-4 py-2">Status</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let asset of visibleAssets" (click)="openEditor(asset)"
            class="cursor-pointer border-b border-border last:border-0 hover:bg-surface-hover">
          <td class="px-4 py-2 font-mono">{{ asset.code }}</td>
          <td class="px-4 py-2 font-medium">{{ asset.title }}</td>
          <td class="px-4 py-2 text-text-secondary">{{ asset.assetTypeName }}</td>
          <td class="px-4 py-2 text-text-secondary">{{ asset.targetLengthSeconds }}</td>
          <td class="px-4 py-2 text-text-secondary">{{ asset.sequenceNumber }}</td>
          <td class="px-4 py-2 text-text-secondary">{{ asset.sequenceNumber }}</td>
          <td class="px-4 py-2">
            <span class="rounded-full px-2 py-0.5 text-xs font-medium text-white" [ngClass]="statusPillClass(asset.status)">
              {{ asset.status }}
            </span>
          </td>
        </tr>
      </tbody>
    </table>
  </div>

  <div *ngIf="selectedAssetForEdit" class="fixed inset-0 z-20 flex items-center justify-center bg-black/50 p-4"
       (click)="closeEditor()">
    <div class="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-lg border border-border bg-surface p-4" (click)="$event.stopPropagation()">
      <app-asset-editor [asset]="selectedAssetForEdit" [assetTypes]="assetTypes" [phases]="phases"
                        [beats]="beatsByEpisode.get(selectedAssetForEdit.episodeId) ?? []"
                        (saved)="onEditorSaved($event)" (cancelled)="closeEditor()"></app-asset-editor>
    </div>
  </div>
</div>
```

Note the "Timeline order" and "Shooting order" columns both currently display `{{ asset.sequenceNumber }}` in
the snippet above — **fix this before using it**: "Timeline order" has no single pre-computed display value on
`AssetDto` the way "Shooting order" does (`sequenceNumber` is a real field; timeline order is *derived* from
linked beats, computed only inside `asset-view.logic.ts`'s sort function, not stored on the DTO). Add a small
display helper to `AssetViewComponent` instead of trying to bind a nonexistent field:

```ts
  timelineOrderDisplay(asset: AssetDto): string {
    const ordinals = asset.beatIds.map((id) => this.beatsById.get(id)?.ordinal).filter((o): o is number => o !== undefined);
    return ordinals.length > 0 ? String(Math.min(...ordinals)) : '—';
  }
```

Add this method to `AssetViewComponent` (in `asset-view.component.ts`) and bind the "Timeline order" `<td>` to
`{{ timelineOrderDisplay(asset) }}` instead of `{{ asset.sequenceNumber }}`. Leave "Shooting order" bound to
`{{ asset.sequenceNumber }}` (that one genuinely is the right field to display).

- [ ] **Step 3: Run the component test suite**

Run: `cd web && npx ng test --watch=false`

Expected: PASS — confirms the template compiles against the component (Angular AOT template type-checking).

- [ ] **Step 4: Rebuild the frontend into wwwroot**

Run: `cd web && npx ng build --output-path=../src/ProductionBible.Api/wwwroot`

Expected: build succeeds with no errors.

- [ ] **Step 5: Run the app and verify manually**

Run: `dotnet run --project src/ProductionBible.Api` (check first whether something is already listening on
port 5280; if so, confirm it's serving the bundle you just built before reusing it, and don't start a second
instance).

Open `http://localhost:5280`, click "Assets" in the nav. Verify:
- Every asset across every episode of the selected project appears in the table.
- Clicking each sortable column header (Title, Type, Length, Timeline order, Shooting order) re-orders the
  rows; clicking the same header twice reverses the order.
- The Status and Shot type filter dropdowns narrow the visible rows; the "To shoot only" checkbox hides
  assets that have a `completedAtUtc` set (if none currently do in the seeded data, verify the filter at
  least doesn't crash and shows the full list — note this in your report rather than treating it as
  unverifiable).
- Clicking a row opens the modal with that asset's fields pre-filled, including its own episode's beat-link
  checkboxes (not another episode's).
- Saving in the modal updates the row in the table and closes the modal.
- Clicking the backdrop (outside the modal panel) closes it without saving.
- Manage > Assets still works exactly as before (edit, save, cancel, create, delete) — confirms the Task 1
  refactor didn't regress it.

If any of these fail, fix before proceeding.

- [ ] **Step 6: Commit the rebuilt wwwroot alongside the template/route/nav changes**

```bash
git add web/src/app/asset-view/asset-view.component.html web/src/app/asset-view/asset-view.component.ts \
        web/src/app/app.routes.ts web/src/app/app.html src/ProductionBible.Api/wwwroot
git commit -m "Add Asset view: sortable/filterable list opening the shared asset editor

Final Track B sub-project. New /assets route and nav link; table
sortable by type/name/length/timeline order/shooting order,
filterable by status/shot type/to-shoot; clicking a row opens the
same AssetEditorComponent Manage > Assets uses, in a modal overlay.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Self-Review Notes

- **Spec coverage:** Task 1 covers the `AssetEditorComponent` extraction. Task 2 covers sort/filter logic.
  Task 3 covers data loading and modal state. Task 4 covers the template, route/nav, rebuild, and manual
  verification. Every spec section maps to a task above.
- **Placeholder scan:** the `Promise.resolve(...) as any` snippet in Task 3 Step 3, and the double
  `{{ asset.sequenceNumber }}` binding in Task 4 Step 2, are deliberately flagged wrong-approach examples
  with the correct replacement given immediately after — not unresolved placeholders. Everything else is
  literal code or an exact command.
- **Type consistency:** `AssetEditorComponent`'s `@Input`/`@Output` names match between Task 1's
  implementation and Task 3/4's `AssetViewComponent`/template usage (`[asset]`, `[assetTypes]`, `[phases]`,
  `[beats]`, `(saved)`, `(cancelled)`). `filterAndSortAssets`'s signature matches between Task 2's
  implementation and Task 3's call site.
- **Scope:** 4 tasks — one refactor (Task 1, of existing tested code, behavior-preserving), three additive
  (Tasks 2-4). No backend changes anywhere in this plan.
