# Production Plan CRUD + Reorder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix GitHub issue #18 (phase-reorder corrupts `Asset.SequenceNumber`), fix a live regression where Production Plan groups assets by an attribute the importer no longer writes, and add drag-reorder for phases and shots-within-a-phase on the Production Plan view.

**Architecture:** A new nullable `Asset.OrderInPhase` column (mirroring `AssetBeat.OrderInBeat`) replaces `SequenceNumber` as the field `ReorderWithinPhaseAsync` writes. `phase-grouping.ts` is rewritten to group by the real `Phase` entity (via `phaseId`) instead of a dead string attribute. `ProductionPlanComponent` gets two CDK `cdkDropList`s (phases, and shots-within-a-phase) mirroring sub-project #4's pattern, with the guards that sub-project #4's final review added in a follow-up round (`isPointerOverContainer`, no-op check) built in from the start.

**Tech Stack:** ASP.NET Core / EF Core 10 (backend), Angular 22 zoneless + `@angular/cdk` (already installed, sub-project #4), RxJS, xUnit (backend tests), Jasmine/Karma (frontend tests).

**Spec:** `docs/superpowers/specs/2026-09-11-production-plan-reorder-design.md`

## Global Constraints

- `Asset.OrderInPhase` is `int?`, server-managed only (never part of `CreateAssetRequest`/`UpdateAssetRequest`) — set exclusively by `ReorderWithinPhaseAsync`, exactly like `AssetBeat.OrderInBeat` is set exclusively by `ReorderWithinBeatAsync`.
- Ordering fallback wherever assets need an initial order within a phase: `OrderInPhase ?? SequenceNumber ?? MAX` — mirrors `BeatService.GetByEpisodeAsync`'s existing `OrderInBeat ?? int.MaxValue` pattern exactly.
- No Importer changes. No changes to `CreateAssetRequest`/`UpdateAssetRequest` (client-facing asset write DTOs stay as they are).
- Two independent, unconnected `cdkDropList`s (phases; shots-within-one-phase, one per phase). The "Unphased" bucket is never inside either drop list — same asymmetry as sub-project #4's "Unassigned" section.
- Every new drag handler starts with, in this order: `if (!event.isPointerOverContainer) return;` then `if (event.previousIndex === event.currentIndex) return;` — both guards are load-bearing (sub-project #4 shipped without them and had to add them in a post-review fix wave; this plan bakes them in from Task 4 onward).
- The app is zoneless (no `provideZoneChangeDetection`) — every state mutation from a `.subscribe()` callback or event handler that should repaint needs an explicit `this.cdr.markForCheck()` call, placed right after the optimistic mutation and before the HTTP call.
- Any change under `web/src/` needs the wwwroot rebuild (`cd web && npx ng build --output-path=../src/ProductionBible.Api/wwwroot`) as its own explicit step, verified by actually running `dotnet run` — this is Task 5, not optional polish.
- Any change under `src/ProductionBible.Application` or `src/ProductionBible.Api` needs `dotnet test ProductionBible.sln` run and green before moving on.

---

## File Structure

- Modify: `src/ProductionBible.Application/Entities/Asset.cs` — add `OrderInPhase` property.
- Create (via `dotnet ef migrations add`): `src/ProductionBible.Application/Migrations/*_AddOrderInPhaseToAsset.cs` + `.Designer.cs`, and update `ProductionBibleDbContextModelSnapshot.cs`.
- Modify: `src/ProductionBible.Application/Dtos/AssetDtos.cs` — add `OrderInPhase` to `AssetDto`.
- Modify: `src/ProductionBible.Application/Services/AssetService.cs` — `ReorderWithinPhaseAsync` writes `OrderInPhase` instead of `SequenceNumber`; `ToDto` maps the new field.
- Modify: `tests/ProductionBible.Application.Tests/AssetServiceTests.cs` — rewrite the two `ReorderWithinPhaseAsync` tests to track `OrderInPhase`, add a test proving `SequenceNumber` survives a reorder untouched.
- Modify: `web/src/app/core/models.ts` — `AssetDto` gains `orderInPhase`.
- Modify: `web/src/app/core/api-client.service.ts` — `reorderPhases`, `reorderAssetsWithinPhase`.
- Modify: `web/src/app/core/api-client.service.spec.ts` — tests for the two new methods.
- Modify: `web/src/app/production-plan/phase-grouping.ts` — rewrite to group by `Phase`/`phaseId`, return `{ phaseGroups, unphasedGroup }`.
- Modify: `web/src/app/production-plan/production-plan.component.ts` — fetch phases, new handlers `onPhaseDrop`/`onShotDrop`, new fields `phaseGroups`/`unphasedGroup` replacing `groups`.
- Modify: `web/src/app/production-plan/production-plan.component.spec.ts` — rewrite `buildPhaseGroups` tests and component tests for the new shape; add handler tests.
- Modify: `web/src/app/production-plan/production-plan.component.html` — `cdkDropList`/`cdkDrag`/`cdkDragHandle` markup, drag-handle table column, separate static "Unphased" section.
- Modify: `src/ProductionBible.Api/wwwroot/browser/**` — rebuilt frontend bundle (Task 5).

---

## Task 1: Backend — add `Asset.OrderInPhase` and fix the reorder-within-phase corruption (issue #18)

**Files:**
- Modify: `src/ProductionBible.Application/Entities/Asset.cs`
- Create: EF migration files (via `dotnet ef migrations add`)
- Modify: `src/ProductionBible.Application/Dtos/AssetDtos.cs`
- Modify: `src/ProductionBible.Application/Services/AssetService.cs`
- Modify: `tests/ProductionBible.Application.Tests/AssetServiceTests.cs`

**Interfaces:**
- Produces: `Asset.OrderInPhase: int?`, `AssetDto.OrderInPhase: int?` (last positional parameter — the only construction site is `AssetService.ToDto`, confirmed by grep before this plan was written) — consumed by Task 2's `models.ts` `AssetDto.orderInPhase` field and by Task 3's `phase-grouping.ts` sort.
- Consumes: nothing new from other tasks (this task is entirely backend and can run independently of Tasks 2-5, though it's ordered first since Task 3's sort depends on the field existing on the DTO).

- [ ] **Step 1: Write the failing tests**

In `tests/ProductionBible.Application.Tests/AssetServiceTests.cs`, replace the existing test
`ReorderWithinPhaseAsync_reassigns_SequenceNumber_to_match_the_given_order` (lines 183-206) with:

```csharp
    [Fact]
    public async Task ReorderWithinPhaseAsync_reassigns_OrderInPhase_to_match_the_given_order()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, _) = await SeedAsync(context);
        var project = (await context.Episodes.FindAsync(episodeId))!.Project;
        var phase = new Phase { Project = project, Name = "Setup A", OrderIndex = 0 };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();
        var service = new AssetService(context);
        var assetA = await service.CreateAsync(episodeId, MinimalRequest(assetTypeId, "A-01"));
        var assetB = await service.CreateAsync(episodeId, MinimalRequest(assetTypeId, "A-02"));
        (await context.Assets.FindAsync(assetA.Id))!.PhaseId = phase.Id;
        (await context.Assets.FindAsync(assetB.Id))!.PhaseId = phase.Id;
        await context.SaveChangesAsync();

        var result = await service.ReorderWithinPhaseAsync(phase.Id, new[] { assetB.Id, assetA.Id });

        Assert.True(result);
        var reorderedB = await service.GetByIdAsync(assetB.Id);
        var reorderedA = await service.GetByIdAsync(assetA.Id);
        Assert.Equal(0, reorderedB!.OrderInPhase);
        Assert.Equal(1, reorderedA!.OrderInPhase);
    }

    [Fact]
    public async Task ReorderWithinPhaseAsync_leaves_SequenceNumber_untouched()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, _) = await SeedAsync(context);
        var project = (await context.Episodes.FindAsync(episodeId))!.Project;
        var phase = new Phase { Project = project, Name = "Setup A", OrderIndex = 0 };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();
        var service = new AssetService(context);
        var assetA = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "A", null, "Planned", null, SequenceNumber: 40, TargetLengthSeconds: null,
            Attributes: null, BeatIds: null));
        var assetB = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-02", "B", null, "Planned", null, SequenceNumber: 41, TargetLengthSeconds: null,
            Attributes: null, BeatIds: null));
        (await context.Assets.FindAsync(assetA.Id))!.PhaseId = phase.Id;
        (await context.Assets.FindAsync(assetB.Id))!.PhaseId = phase.Id;
        await context.SaveChangesAsync();

        var result = await service.ReorderWithinPhaseAsync(phase.Id, new[] { assetB.Id, assetA.Id });

        Assert.True(result);
        var reorderedA = await service.GetByIdAsync(assetA.Id);
        var reorderedB = await service.GetByIdAsync(assetB.Id);
        Assert.Equal(40, reorderedA!.SequenceNumber);
        Assert.Equal(41, reorderedB!.SequenceNumber);
    }
```

Also update `ReorderWithinPhaseAsync_rejects_a_duplicate_id_and_writes_nothing` (lines 230-253): change its two
final assertions from `Assert.Equal(assetA.SequenceNumber, unchangedA!.SequenceNumber);` /
`Assert.Equal(assetB.SequenceNumber, unchangedB!.SequenceNumber);` to:

```csharp
        Assert.Null(unchangedA!.OrderInPhase);
        Assert.Null(unchangedB!.OrderInPhase);
```

(Both assets were created via `MinimalRequest`, which passes no `OrderInPhase` — it doesn't exist as a
request field — so it starts and, after a rejected reorder, must remain `null`.)

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test ProductionBible.sln --filter AssetServiceTests`

Expected: FAIL — `AssetDto` has no `OrderInPhase` member yet (compile error), and `Asset` has no
`OrderInPhase` property yet.

- [ ] **Step 3: Add the entity property**

In `src/ProductionBible.Application/Entities/Asset.cs`, add right after `public int? PhaseId { get; set; }`:

```csharp
    public int? OrderInPhase { get; set; }
```

- [ ] **Step 4: Add the DTO field**

In `src/ProductionBible.Application/Dtos/AssetDtos.cs`, add `OrderInPhase` as the **last** parameter of
`AssetDto` (after `BeatIds`):

```csharp
public record AssetDto(
    int Id,
    int EpisodeId,
    int AssetTypeId,
    string AssetTypeName,
    string Code,
    string Title,
    string? ScriptText,
    string Status,
    string? Notes,
    int? SequenceNumber,
    int? TargetLengthSeconds,
    int? PhaseId,
    DateTime? CompletedAtUtc,
    Dictionary<string, string> Attributes,
    int[] BeatIds,
    int? OrderInPhase);
```

Do not add `OrderInPhase` to `CreateAssetRequest` or `UpdateAssetRequest` — it is server-managed only, exactly
like `AssetBeat.OrderInBeat`.

- [ ] **Step 5: Fix `ReorderWithinPhaseAsync` and update `ToDto`**

In `src/ProductionBible.Application/Services/AssetService.cs`:

Change `ReorderWithinPhaseAsync`'s assignment line (currently
`assetsById[orderedAssetIds[i]].SequenceNumber = i;`) to:

```csharp
            assetsById[orderedAssetIds[i]].OrderInPhase = i;
```

Change the `ToDto` method's final line (currently ending `asset.AssetBeats.Select(ab => ab.BeatId).ToArray());`)
to append the new field:

```csharp
        asset.AssetBeats.Select(ab => ab.BeatId).ToArray(),
        asset.OrderInPhase);
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test ProductionBible.sln --filter AssetServiceTests`

Expected: PASS. These tests use EF Core's InMemory provider (`CreateInMemoryContext`), which builds its
schema directly from the entity classes and `OnModelCreating` — not from applied migrations — so Steps 3-5
alone (entity property, DTO field, service fix) are sufficient to make these tests pass. The migration
created in Step 7 below is what a real SQLite database needs; it is not required for these unit tests to
pass.

- [ ] **Step 7: Create the EF migration**

Run: `dotnet ef migrations add AddOrderInPhaseToAsset --project src/ProductionBible.Application --startup-project src/ProductionBible.Api`

Expected: creates `src/ProductionBible.Application/Migrations/<timestamp>_AddOrderInPhaseToAsset.cs` and its
`.Designer.cs`, and updates `ProductionBibleDbContextModelSnapshot.cs`. This is what makes a real SQLite
database (not the in-memory test provider) gain the column — `Program.cs:41` already calls
`db.Database.Migrate()` on every app startup, so no manual reseed step is needed for existing databases.

- [ ] **Step 8: Run the full backend test suite**

Run: `dotnet test ProductionBible.sln`

Expected: all tests pass (Application, Importer, Api test projects).

- [ ] **Step 9: Commit**

```bash
git add src/ProductionBible.Application/Entities/Asset.cs \
        src/ProductionBible.Application/Migrations/ \
        src/ProductionBible.Application/Dtos/AssetDtos.cs \
        src/ProductionBible.Application/Services/AssetService.cs \
        tests/ProductionBible.Application.Tests/AssetServiceTests.cs
git commit -m "Fix reorder-within-phase corrupting SequenceNumber (issue #18)

Adds Asset.OrderInPhase (mirrors AssetBeat.OrderInBeat) and points
ReorderWithinPhaseAsync at it instead of the global page-number field
it was overwriting.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 2: Frontend — model field and `ApiClientService` reorder methods

**Files:**
- Modify: `web/src/app/core/models.ts`
- Modify: `web/src/app/core/api-client.service.ts`
- Modify: `web/src/app/core/api-client.service.spec.ts`

**Interfaces:**
- Consumes: `AssetDto.OrderInPhase` (Task 1, backend — the wire field is camelCased by ASP.NET's default
  JSON serialization to `orderInPhase`).
- Produces: `AssetDto.orderInPhase: number | null` (TS), `ApiClientService.reorderPhases(projectId: number,
  orderedIds: number[]): Observable<void>`, `ApiClientService.reorderAssetsWithinPhase(phaseId: number,
  orderedIds: number[]): Observable<void>` — both consumed by Task 4's `ProductionPlanComponent`.

- [ ] **Step 1: Add the model field**

In `web/src/app/core/models.ts`, add to the `AssetDto` interface (after `beatIds: number[];`):

```ts
  orderInPhase: number | null;
```

- [ ] **Step 2: Write the failing API client tests**

Add to `web/src/app/core/api-client.service.spec.ts`, after the existing `reorderAssetsWithinBeat` test
(added by sub-project #4, before the closing `});` of the `describe` block):

```ts
  it('sends a PATCH to /api/projects/{projectId}/phases/reorder for reorderPhases', () => {
    service.reorderPhases(1, [5, 3, 4]).subscribe();
    const req = httpMock.expectOne('/api/projects/1/phases/reorder');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ orderedIds: [5, 3, 4] });
    req.flush(null);
  });

  it('sends a PATCH to /api/phases/{phaseId}/assets/reorder for reorderAssetsWithinPhase', () => {
    service.reorderAssetsWithinPhase(5, [1002, 1000, 1001]).subscribe();
    const req = httpMock.expectOne('/api/phases/5/assets/reorder');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ orderedIds: [1002, 1000, 1001] });
    req.flush(null);
  });
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `cd web && npx ng test --watch=false`

Expected: FAIL — `service.reorderPhases is not a function` (and same for `reorderAssetsWithinPhase`).

- [ ] **Step 4: Implement the methods**

In `web/src/app/core/api-client.service.ts`, add to the end of the `ApiClientService` class body (after the
`reorderAssetsWithinBeat` method sub-project #4 added):

```ts
  reorderPhases(projectId: number, orderedIds: number[]): Observable<void> {
    return this.http.patch<void>(`/api/projects/${projectId}/phases/reorder`, { orderedIds });
  }

  reorderAssetsWithinPhase(phaseId: number, orderedIds: number[]): Observable<void> {
    return this.http.patch<void>(`/api/phases/${phaseId}/assets/reorder`, { orderedIds });
  }
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `cd web && npx ng test --watch=false`

Expected: PASS, all tests including the two new ones. (This will also currently fail to compile until Task 1's
backend change lands and this task's own `models.ts` edit is in place — if run before Task 1's migration, the
frontend build itself does not depend on the backend migration having run; it only depends on the TS
`AssetDto.orderInPhase` field existing, added in Step 1 above.)

- [ ] **Step 6: Commit**

```bash
git add web/src/app/core/models.ts web/src/app/core/api-client.service.ts web/src/app/core/api-client.service.spec.ts
git commit -m "Add orderInPhase field and phase/within-phase reorder methods to ApiClientService

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 3: Frontend — rewrite `phase-grouping.ts` to use the real `Phase` entity

**Files:**
- Modify: `web/src/app/production-plan/phase-grouping.ts`
- Modify: `web/src/app/production-plan/production-plan.component.spec.ts` (the `buildPhaseGroups` describe
  block only — the `ProductionPlanComponent` describe block is Task 4)

**Interfaces:**
- Consumes: `AssetDto.phaseId`, `AssetDto.orderInPhase`, `AssetDto.sequenceNumber` (all pre-existing except
  `orderInPhase`, added in Task 2); `PhaseDto { id, projectId, name, orderIndex }` (pre-existing, from
  sub-project #3).
- Produces: `buildPhaseGroups(assets: AssetDto[], phases: PhaseDto[]): { phaseGroups: PhaseGroup[]; unphasedGroup: PhaseGroup | null }`
  where `PhaseGroup = { phaseId: number | null; phase: string; assets: AssetDto[] }` — consumed by Task 4's
  `ProductionPlanComponent`.

- [ ] **Step 1: Write the failing tests**

Replace the entire `describe('buildPhaseGroups', ...)` block in
`web/src/app/production-plan/production-plan.component.spec.ts` (lines 18-43) with:

```ts
function asset(
  id: number, code: string, sequenceNumber: number | null, phaseId: number | null,
  episodeId = 10, orderInPhase: number | null = null,
): AssetDto {
  return {
    id, episodeId, assetTypeId: 1, assetTypeName: 'Shot', code, title: code,
    scriptText: null, status: 'Planned', notes: null, sequenceNumber, targetLengthSeconds: null,
    phaseId, orderInPhase, completedAtUtc: null, attributes: {}, beatIds: [],
  };
}

describe('buildPhaseGroups', () => {
  const phase1: PhaseDto = { id: 1, projectId: 1, name: 'Phase 1: The software time machine', orderIndex: 0 };
  const phase2: PhaseDto = { id: 2, projectId: 1, name: 'Phase 2: Makerspace trip', orderIndex: 1 };

  it('groups assets by phaseId, not by any attribute', () => {
    const { phaseGroups } = buildPhaseGroups(
      [asset(2, 'F-01', 1, phase1.id), asset(1, 'B-02', 2, phase2.id)],
      [phase1, phase2],
    );
    expect(phaseGroups.map((g) => g.assets.map((a) => a.code))).toEqual([['F-01'], ['B-02']]);
  });

  it('orders phase groups by Phase.orderIndex, not by first-seen order in the asset list', () => {
    const { phaseGroups } = buildPhaseGroups(
      [asset(1, 'B-02', 2, phase2.id), asset(2, 'F-01', 1, phase1.id)],
      [phase1, phase2],
    );
    expect(phaseGroups.map((g) => g.phase)).toEqual([
      'Phase 1: The software time machine',
      'Phase 2: Makerspace trip',
    ]);
  });

  it('orders assets within a phase by orderInPhase, falling back to sequenceNumber', () => {
    const { phaseGroups } = buildPhaseGroups(
      [
        asset(1, 'A-01', 10, phase1.id, 10, null),
        asset(2, 'A-02', 5, phase1.id, 10, 1),
        asset(3, 'A-03', 1, phase1.id, 10, 0),
      ],
      [phase1],
    );
    expect(phaseGroups[0].assets.map((a) => a.code)).toEqual(['A-03', 'A-02', 'A-01']);
  });

  it('buckets assets with no phaseId into a separate unphasedGroup', () => {
    const { phaseGroups, unphasedGroup } = buildPhaseGroups([asset(3, 'X-01', 1, null)], []);
    expect(phaseGroups).toEqual([]);
    expect(unphasedGroup).not.toBeNull();
    expect(unphasedGroup!.assets.map((a) => a.code)).toEqual(['X-01']);
    expect(unphasedGroup!.phaseId).toBeNull();
  });

  it('returns a null unphasedGroup when every asset has a phaseId', () => {
    const { unphasedGroup } = buildPhaseGroups([asset(1, 'A-01', 1, phase1.id)], [phase1]);
    expect(unphasedGroup).toBeNull();
  });

  it('includes a phase group with an empty assets array for a phase with no linked assets yet', () => {
    const { phaseGroups } = buildPhaseGroups([], [phase1]);
    expect(phaseGroups).toEqual([{ phaseId: phase1.id, phase: phase1.name, assets: [] }]);
  });
});
```

This also requires changing the top of the file's imports (line 7) from
`import { AssetDto, EpisodeDto, ProjectDto } from '../core/models';` to add `PhaseDto`:

```ts
import { AssetDto, EpisodeDto, PhaseDto, ProjectDto } from '../core/models';
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd web && npx ng test --watch=false`

Expected: FAIL — `buildPhaseGroups` still takes one argument and returns an array, not
`{ phaseGroups, unphasedGroup }` (type errors / assertion failures).

- [ ] **Step 3: Rewrite `phase-grouping.ts`**

Replace the entire contents of `web/src/app/production-plan/phase-grouping.ts` with:

```ts
import { AssetDto, PhaseDto } from '../core/models';

export interface PhaseGroup {
  phaseId: number | null;
  phase: string;
  assets: AssetDto[];
}

export function buildPhaseGroups(
  assets: AssetDto[],
  phases: PhaseDto[],
): { phaseGroups: PhaseGroup[]; unphasedGroup: PhaseGroup | null } {
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

- [ ] **Step 4: Run the tests to verify they pass**

Run: `cd web && npx ng test --watch=false`

Expected: `buildPhaseGroups`'s 6 tests pass. The `ProductionPlanComponent` describe block (below this one in
the same spec file) will now fail to compile/pass — that's expected, Task 4 fixes it. Confirm the failures
are confined to that block (compile errors referencing `component.groups` / the old `apiSpy.getAssets`-only
stub shape) and not inside `buildPhaseGroups`'s own tests.

- [ ] **Step 5: Commit**

```bash
git add web/src/app/production-plan/phase-grouping.ts web/src/app/production-plan/production-plan.component.spec.ts
git commit -m "Rewrite phase-grouping.ts to group by the real Phase entity, not a dead attribute

The importer stopped writing attributes['PhaseGroup'] when Phase
became a first-class entity (PR #23) — this view had never been
updated to match, so every asset in a post-migration database was
silently bucketed as Unphased.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

(Note: this commit leaves `ProductionPlanComponent`'s own tests red — Task 4 fixes them in the next commit.
This is intentional: the plan splits the grouping-logic rewrite from the component/template rewrite into
separate, independently-reviewable commits, matching the file-per-responsibility structure above. If your
process requires every commit to leave the suite green, squash Tasks 3 and 4 into one commit instead — but
implement and verify them as the two separate steps described here regardless.)

---

## Task 4: Frontend — wire `ProductionPlanComponent`'s drop handlers

**Files:**
- Modify: `web/src/app/production-plan/production-plan.component.ts`
- Modify: `web/src/app/production-plan/production-plan.component.spec.ts` (the `ProductionPlanComponent`
  describe block)

**Interfaces:**
- Consumes: `buildPhaseGroups` (Task 3), `ApiClientService.getPhases`/`reorderPhases`/
  `reorderAssetsWithinPhase` (Task 2 for the two reorder methods; `getPhases` pre-existing from sub-project
  #3), `PhaseGroup` (Task 3).
- Produces: `ProductionPlanComponent.onPhaseDrop(event: CdkDragDrop<PhaseGroup[]>): void`,
  `ProductionPlanComponent.onShotDrop(group: PhaseGroup, event: CdkDragDrop<AssetDto[]>): void`, fields
  `phaseGroups: PhaseGroup[]` and `unphasedGroup: PhaseGroup | null` (replacing the old `groups` field) —
  consumed by Task 5's template.

- [ ] **Step 1: Write the failing tests**

Replace the entire `describe('ProductionPlanComponent', ...)` block in
`web/src/app/production-plan/production-plan.component.spec.ts` with:

```ts
describe('ProductionPlanComponent', () => {
  let fixture: ComponentFixture<ProductionPlanComponent>;
  let component: ProductionPlanComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode1: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const episode2: EpisodeDto = { id: 11, projectId: 1, name: 'EP2', orderIndex: 2 };
  const phase1: PhaseDto = { id: 1, projectId: 1, name: 'Phase 1: The software time machine', orderIndex: 0 };
  const phase2: PhaseDto = { id: 2, projectId: 1, name: 'Phase 2: Makerspace trip', orderIndex: 1 };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', [
      'getEpisodes', 'getAssets', 'getPhases', 'reorderPhases', 'reorderAssetsWithinPhase',
    ]);
    apiSpy.getEpisodes.and.returnValue(of([episode1, episode2]));
    apiSpy.getPhases.and.returnValue(of([phase1, phase2]));
    apiSpy.getAssets.and.callFake((episodeId: number) => {
      if (episodeId === episode1.id) {
        return of([asset(2, 'F-01', 1, phase1.id, episode1.id)]);
      }
      return of([asset(1, 'B-02', 2, phase2.id, episode2.id)]);
    });

    const projectContextStub = {
      projects: signal<ProjectDto[]>([project]),
      selectedProjectId: signal<number | null>(project.id),
      selectProject: jasmine.createSpy('selectProject'),
    };

    await TestBed.configureTestingModule({
      imports: [ProductionPlanComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: projectContextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductionPlanComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    fixture.detectChanges();
  });

  it('loads phase groups scoped to the real Phase entities for the selected project', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(apiSpy.getPhases).toHaveBeenCalledWith(1);
    expect(component.phaseGroups.map((g) => g.phase)).toEqual([
      'Phase 1: The software time machine',
      'Phase 2: Makerspace trip',
    ]);
  });

  it('merges assets from every episode in the project', () => {
    const allCodes = component.phaseGroups.flatMap((g) => g.assets.map((a) => a.code));
    expect(allCodes).toEqual(['F-01', 'B-02']);
  });

  it('reorders phases in place and calls reorderPhases with the new order, ignoring a drop outside the container', () => {
    apiSpy.reorderPhases.and.returnValue(of(undefined));

    component.onPhaseDrop({ previousIndex: 0, currentIndex: 1, isPointerOverContainer: false } as any);
    expect(apiSpy.reorderPhases).not.toHaveBeenCalled();

    component.onPhaseDrop({ previousIndex: 0, currentIndex: 1, isPointerOverContainer: true } as any);
    expect(component.phaseGroups.map((g) => g.phaseId)).toEqual([phase2.id, phase1.id]);
    expect(apiSpy.reorderPhases).toHaveBeenCalledWith(1, [phase2.id, phase1.id]);
  });

  it('ignores a no-op phase drop (previousIndex === currentIndex)', () => {
    apiSpy.reorderPhases.and.returnValue(of(undefined));
    component.onPhaseDrop({ previousIndex: 0, currentIndex: 0, isPointerOverContainer: true } as any);
    expect(apiSpy.reorderPhases).not.toHaveBeenCalled();
  });

  it("reorders a phase's shots in place and calls reorderAssetsWithinPhase with the new order", () => {
    const group = { phaseId: phase1.id, phase: phase1.name, assets: [
      asset(1, 'A-01', 1, phase1.id), asset(2, 'A-02', 2, phase1.id),
    ] };
    apiSpy.reorderAssetsWithinPhase.and.returnValue(of(undefined));

    component.onShotDrop(group, { previousIndex: 0, currentIndex: 1, isPointerOverContainer: true } as any);

    expect(group.assets.map((a) => a.id)).toEqual([2, 1]);
    expect(apiSpy.reorderAssetsWithinPhase).toHaveBeenCalledWith(phase1.id, [2, 1]);
  });
});
```

This requires updating the file's imports at the top to add `PhaseDto` (if not already added in Task 3) and
`CdkDragDrop` is not needed in the spec file itself since the tests construct plain object literals cast
`as any`, matching sub-project #4's established test pattern.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd web && npx ng test --watch=false`

Expected: FAIL — `component.phaseGroups` doesn't exist yet (still `component.groups`), `onPhaseDrop`/
`onShotDrop` don't exist yet, `apiSpy` has no `getPhases`/`reorderPhases`/`reorderAssetsWithinPhase` stubs
wired to real behavior yet.

- [ ] **Step 3: Implement the component changes**

Replace the full contents of `web/src/app/production-plan/production-plan.component.ts` with:

```ts
import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { statusPillClass } from '../core/status-style';
import { buildPhaseGroups, PhaseGroup } from './phase-grouping';
import { AssetDto } from '../core/models';

@Component({
  selector: 'app-production-plan',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  templateUrl: './production-plan.component.html',
})
export class ProductionPlanComponent {
  phaseGroups: PhaseGroup[] = [];
  unphasedGroup: PhaseGroup | null = null;
  protected readonly statusPillClass = statusPillClass;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      if (projectId !== null) {
        this.loadGroups(projectId);
      }
    });
  }

  onPhaseDrop(event: CdkDragDrop<PhaseGroup[]>): void {
    if (!event.isPointerOverContainer) return;
    if (event.previousIndex === event.currentIndex) return;
    const projectId = this.projectContext.selectedProjectId();
    if (projectId === null) return;
    moveItemInArray(this.phaseGroups, event.previousIndex, event.currentIndex);
    const orderedIds = this.phaseGroups.map((g) => g.phaseId!);
    this.cdr.markForCheck();
    this.api.reorderPhases(projectId, orderedIds).subscribe(() => {
      this.loadGroups(projectId);
    });
  }

  onShotDrop(group: PhaseGroup, event: CdkDragDrop<AssetDto[]>): void {
    if (!event.isPointerOverContainer) return;
    if (event.previousIndex === event.currentIndex) return;
    if (group.phaseId === null) return;
    const phaseId = group.phaseId;
    const projectId = this.projectContext.selectedProjectId();
    moveItemInArray(group.assets, event.previousIndex, event.currentIndex);
    const orderedIds = group.assets.map((a) => a.id);
    this.cdr.markForCheck();
    this.api.reorderAssetsWithinPhase(phaseId, orderedIds).subscribe(() => {
      if (projectId !== null) this.loadGroups(projectId);
    });
  }

  private loadGroups(projectId: number): void {
    forkJoin({
      episodes: this.api.getEpisodes(projectId),
      phases: this.api.getPhases(projectId),
    }).subscribe(({ episodes, phases }) => {
      if (this.projectContext.selectedProjectId() !== projectId) return;
      if (episodes.length === 0) {
        this.phaseGroups = [];
        this.unphasedGroup = null;
        this.cdr.markForCheck();
        return;
      }

      forkJoin(episodes.map((episode) => this.api.getAssets(episode.id))).subscribe((assetLists) => {
        if (this.projectContext.selectedProjectId() !== projectId) return;
        const { phaseGroups, unphasedGroup } = buildPhaseGroups(assetLists.flat(), phases);
        this.phaseGroups = phaseGroups;
        this.unphasedGroup = unphasedGroup;
        this.cdr.markForCheck();
      });
      this.cdr.markForCheck();
    });
  }
}
```


- [ ] **Step 4: Run the tests to verify they pass**

Run: `cd web && npx ng test --watch=false`

Expected: PASS — both the `buildPhaseGroups` tests (Task 3) and the `ProductionPlanComponent` tests (this
task) all green.

- [ ] **Step 5: Commit**

```bash
git add web/src/app/production-plan/production-plan.component.ts web/src/app/production-plan/production-plan.component.spec.ts
git commit -m "Add phase and shot-within-phase drag-reorder handlers to ProductionPlanComponent

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 5: Wire the template, rebuild, and verify end-to-end

**Files:**
- Modify: `web/src/app/production-plan/production-plan.component.html`
- Modify: `src/ProductionBible.Api/wwwroot/browser/**` (rebuilt bundle)

**Interfaces:**
- Consumes: `ProductionPlanComponent.phaseGroups`/`unphasedGroup`/`onPhaseDrop`/`onShotDrop` (Task 4).

- [ ] **Step 1: Replace the template**

Replace the full contents of `web/src/app/production-plan/production-plan.component.html` with:

```html
<div class="space-y-6">
  <div cdkDropList [cdkDropListData]="phaseGroups" (cdkDropListDropped)="onPhaseDrop($event)" class="space-y-6">
    <section class="rounded-lg border border-border bg-surface" *ngFor="let group of phaseGroups" cdkDrag>
      <h3 class="sticky top-0 flex items-center gap-2 rounded-t-lg border-b border-border bg-surface px-4 py-3 text-sm font-semibold text-text-secondary">
        <span cdkDragHandle class="cursor-grab select-none" title="Drag to reorder phase">&#8942;&#8942;</span>
        {{ group.phase }}
      </h3>
      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead>
            <tr class="border-b border-border text-xs uppercase tracking-wide text-text-secondary">
              <th class="w-8 px-2 py-2"></th>
              <th class="px-4 py-2">Seq</th>
              <th class="px-4 py-2">Code</th>
              <th class="px-4 py-2">Title</th>
              <th class="px-4 py-2">Location</th>
              <th class="px-4 py-2">Angle &amp; camera</th>
              <th class="px-4 py-2">Audio</th>
              <th class="px-4 py-2">Status</th>
            </tr>
          </thead>
          <tbody [cdkDropListData]="group.assets" cdkDropList (cdkDropListDropped)="onShotDrop(group, $event)">
            <tr *ngFor="let asset of group.assets" class="border-b border-border last:border-0 hover:bg-surface-hover" cdkDrag>
              <td class="px-2 py-2">
                <span cdkDragHandle class="cursor-grab select-none text-text-secondary" title="Drag to reorder shot">&#8942;&#8942;</span>
              </td>
              <td class="px-4 py-2 text-text-secondary">{{ asset.sequenceNumber }}</td>
              <td class="px-4 py-2 font-mono">{{ asset.code }}</td>
              <td class="px-4 py-2 font-medium">{{ asset.title }}</td>
              <td class="px-4 py-2 text-text-secondary">{{ asset.attributes['Location'] }}</td>
              <td class="px-4 py-2 text-text-secondary">{{ asset.attributes['AngleAndCamera'] }}</td>
              <td class="px-4 py-2 text-text-secondary">{{ asset.attributes['AudioNotes'] }}</td>
              <td class="px-4 py-2">
                <span class="rounded-full px-2 py-0.5 text-xs font-medium text-white" [ngClass]="statusPillClass(asset.status)">
                  {{ asset.status }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>

  <section class="rounded-lg border border-border bg-surface" *ngIf="unphasedGroup">
    <h3 class="sticky top-0 rounded-t-lg border-b border-border bg-surface px-4 py-3 text-sm font-semibold text-text-secondary">
      {{ unphasedGroup.phase }}
    </h3>
    <div class="overflow-x-auto">
      <table class="w-full text-left text-sm">
        <thead>
          <tr class="border-b border-border text-xs uppercase tracking-wide text-text-secondary">
            <th class="px-4 py-2">Seq</th>
            <th class="px-4 py-2">Code</th>
            <th class="px-4 py-2">Title</th>
            <th class="px-4 py-2">Location</th>
            <th class="px-4 py-2">Angle &amp; camera</th>
            <th class="px-4 py-2">Audio</th>
            <th class="px-4 py-2">Status</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let asset of unphasedGroup.assets" class="border-b border-border last:border-0 hover:bg-surface-hover">
            <td class="px-4 py-2 text-text-secondary">{{ asset.sequenceNumber }}</td>
            <td class="px-4 py-2 font-mono">{{ asset.code }}</td>
            <td class="px-4 py-2 font-medium">{{ asset.title }}</td>
            <td class="px-4 py-2 text-text-secondary">{{ asset.attributes['Location'] }}</td>
            <td class="px-4 py-2 text-text-secondary">{{ asset.attributes['AngleAndCamera'] }}</td>
            <td class="px-4 py-2 text-text-secondary">{{ asset.attributes['AudioNotes'] }}</td>
            <td class="px-4 py-2">
              <span class="rounded-full px-2 py-0.5 text-xs font-medium text-white" [ngClass]="statusPillClass(asset.status)">
                {{ asset.status }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</div>
```

Notes on this template versus the original:
- The outer `*ngFor="let group of groups"` becomes `*ngFor="let group of phaseGroups"` inside a new
  `<div cdkDropList>` wrapper (with `space-y-6` to preserve the original inter-section spacing, since the
  sections are no longer direct children of the outermost `<div class="space-y-6">`).
  `[cdkDropListData]="phaseGroups"` is bound so `CdkDragDrop<PhaseGroup[]>`'s generic is accurate from the
  start (sub-project #4 had to add this in a post-review fix wave — this plan bakes it in).
  `phaseGroups` items are real `PhaseGroup` objects (not raw ids), so this binding is straightforward, unlike
  sub-project #4's `beat.assetIds: number[]` case.
- Each phase `<h3>` gains a drag-handle span, in a new flex row.
  Each phase `<table>` gains a new narrow first `<th>`/`<td>` column (no header text, just a
  `w-8` spacer) holding the shot-level drag handle.
  The `<tbody>` gains `cdkDropList`/`[cdkDropListData]="group.assets"`/`(cdkDropListDropped)`, and each
  `<tr>` gains `cdkDrag`.
- The "Unphased" section (from `unphasedGroup`) is an entirely separate, static `<section>` — a near-exact
  copy of the original single-group template, minus any `cdkDropList`/`cdkDrag` attributes and minus the
  handle column (nothing here is draggable). It sits outside the outer `cdkDropList`, exactly like
  Storyboard's "Unassigned" section relative to its beats `cdkDropList`.
- `&#8942;&#8942;` renders as two vertical-ellipsis characters (⋮⋮), the same drag-handle glyph sub-project
  #4 used, for visual consistency across both reorderable views in this app.

- [ ] **Step 2: Run the component test suite**

Run: `cd web && npx ng test --watch=false`

Expected: PASS — confirms the template compiles against the component (Angular AOT template type-checking),
including the `[cdkDropListData]` bindings against `PhaseGroup[]` and `AssetDto[]`.

- [ ] **Step 3: Rebuild the frontend into wwwroot**

Run: `cd web && npx ng build --output-path=../src/ProductionBible.Api/wwwroot`

Expected: build succeeds with no errors.

- [ ] **Step 4: Run the backend test suite once more**

Run: `dotnet test ProductionBible.sln`

Expected: all passing — confirms Task 1's migration didn't regress anything and the full stack builds
together.

- [ ] **Step 5: Run the app and verify manually**

Run: `dotnet run --project src/ProductionBible.Api` (check first whether something is already listening on
port 5280 before starting a second instance, and if so, confirm it's serving the bundle you just built rather
than reusing a stale one).

Open `http://localhost:5280`, navigate to Production Plan. Verify:
- Assets that used to show under "Unphased" (before this change) now show under their real phase names —
  this is the live-regression fix; if the seeded database predates PR #23's `Phase` entity or was never
  reimported since, phases may legitimately be empty or absent; note what the actual seeded data shows
  rather than assuming a specific phase layout.
- Each phase section and each shot row shows a `⋮⋮` drag handle.
- Dragging a phase by its handle to a new position reorders it, and the order persists across switching to
  another project and back (or a page refresh), confirming `reorderPhases` round-tripped.
- Dragging a shot by its handle to a new position within its own phase reorders it, and the order persists
  the same way, confirming `reorderAssetsWithinPhase` round-tripped and did **not** change the "Seq" column
  values (the fix for issue #18 — check this specifically, since the bug it fixes was exactly this column
  changing unexpectedly).
- Dragging a shot does not offer to drop it into a different phase's table (the two `cdkDropList`s per phase
  are independent).
- If browser automation tooling is available and a real physical drag proves unreliable to complete after
  2-3 attempts (a known limitation encountered during sub-project #4 — synthetic pointer events can trigger
  native text selection instead of engaging CDK's drag), don't loop indefinitely: report it as
  inconclusive-via-automation rather than a confirmed defect, and rely on Task 4's unit tests (which call the
  handlers directly) as the source of truth for handler correctness, exactly as sub-project #4's final review
  accepted for the same reason.

If any of these fail, fix before proceeding.

- [ ] **Step 6: Commit the rebuilt wwwroot alongside the template change**

```bash
git add web/src/app/production-plan/production-plan.component.html src/ProductionBible.Api/wwwroot
git commit -m "Wire CDK drag-drop into the Production Plan view for phase and shot reorder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Self-Review Notes

- **Spec coverage:** Task 1 covers issue #18's backend fix. Tasks 2-4 cover the frontend rewrite (model
  field, API client, grouping logic, component handlers). Task 5 covers template wiring, rebuild, and manual
  verification. Every spec section (the two bug fixes, backend field/migration, frontend grouping rewrite,
  drag-reorder interaction/guards, API client additions, models.ts additions, testing) maps to a task above.
- **Placeholder scan:** no TBD/TODO; every step has literal code or an exact command.
- **Type consistency:** `PhaseGroup` (`{ phaseId, phase, assets }`) is defined once in Task 3 and used
  identically in Task 4's component and Task 5's template. `onPhaseDrop(event: CdkDragDrop<PhaseGroup[]>)` /
  `onShotDrop(group: PhaseGroup, event: CdkDragDrop<AssetDto[]>)` match between Task 4's implementation, its
  tests, and Task 5's template bindings.
- **Guards learned from sub-project #4 applied from the start:** `isPointerOverContainer` and
  `previousIndex === currentIndex` guards are in Task 4's initial implementation (not a follow-up fix wave),
  per the spec's explicit callout of sub-project #4's final-review findings.
- **Scope:** 5 tasks, spanning backend (Task 1) and frontend (Tasks 2-5), each independently testable and
  committable. No changes to the Importer, to Manage's existing Phase/Asset CRUD forms, or to Storyboard.
