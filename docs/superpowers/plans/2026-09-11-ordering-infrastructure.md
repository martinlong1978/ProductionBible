# Ordering Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give `Beat` a persisted, independently-editable order (`Ordinal`) and
duration (`DurationSeconds`) instead of deriving order from parsing
`Timecode` text; promote `Phase` from an `Asset` attribute string to a real
entity; add reorder endpoints for beats, phases, shots-within-phase, and
shots-within-beat.

**Architecture:** Backend-only (no Angular changes — see Global Constraints).
`Beat.Timecode` is renamed to `SourceTimecode` internally (read-only
reference, still the import dedup key) while `BeatDto`'s outward `Timecode`
field is preserved unchanged so no existing frontend code breaks. New
`Ordinal`/`DurationSeconds` fields on `Beat` are computed once at import time
(porting `timeline-model.ts`'s existing gap-to-next-beat formula to C#) and
then freely editable. `Phase` becomes a first-class entity scoped to
`Project`, matching `Episode`'s existing shape. Four new reorder endpoints
bulk-reassign an ordinal field across a parent's children in one transaction.

**Tech Stack:** ASP.NET Core, EF Core (SQLite), xUnit.

**Spec:** `docs/superpowers/specs/2026-09-11-ordering-infrastructure-design.md`

## Global Constraints

- **No Angular changes in this plan.** Nothing under `web/` is touched. New
  DTO fields are additive only.
- **`BeatDto`'s `Timecode` field name and value never change** — it keeps
  reading from the renamed `SourceTimecode` entity property. Only the C#
  entity property is renamed.
- **Delete-and-reseed, no data migration.** After each schema-changing
  migration, `App_Data/productionbible.db*` gets deleted and the app/importer
  reseed from scratch. No migration writes data-preservation logic.
- **EF migrations use `--startup-project src/ProductionBible.Application`**
  (not `src/ProductionBible.Api` — Api has no
  `Microsoft.EntityFrameworkCore.Design` reference; this bit a prior plan).
- **Reorder endpoints are one `SaveChangesAsync` each** — no partially-applied
  reorder should ever be visible.
- **This project has one contributor.** Skip re-running the full test suite
  a second time after a routine local merge to `main`; running it once before
  merging is still expected per task.

---

### Task 1: Beat gains Ordinal, DurationSeconds, and a renamed SourceTimecode

**Files:**
- Modify: `src/ProductionBible.Application/Entities/Beat.cs`
- Modify: `src/ProductionBible.Application/Dtos/BeatDtos.cs`
- Modify: `src/ProductionBible.Application/Services/BeatService.cs`
- Modify: `src/ProductionBible.Importer/ImportMapper.cs:215`
- Modify: `tests/ProductionBible.Application.Tests/AssetServiceTests.cs` (its `SeedAsync` helper)
- Modify: `tests/ProductionBible.Application.Tests/BeatServiceTests.cs` (already exists — fix compile breaks and remove two now-obsolete tests, see Step 7)
- Modify: `tests/ProductionBible.Importer.Tests/ImportMapperTests.cs`
- Modify: `tests/ProductionBible.Importer.Tests/ImportMapperSqliteTests.cs`
- Create (via `dotnet ef migrations add`): a new migration under `src/ProductionBible.Application/Migrations/`
- Test: add a new test class to `tests/ProductionBible.Application.Tests/BeatServiceTests.cs` (see Step 2)

**Interfaces:**
- Consumes: `TimecodeOrdering.TryParseSeconds(string, out int)` (unchanged, already exists).
- Produces: `Beat.Ordinal` (`int`), `Beat.DurationSeconds` (`int`), `Beat.SourceTimecode` (`string`, renamed from `Timecode`). `BeatDto` gains `int Ordinal`, `int DurationSeconds`, `int StartSeconds`, `int EndSeconds` as new trailing fields — existing `Timecode` field unchanged. `CreateBeatRequest`/`UpdateBeatRequest` gain `int Ordinal, int DurationSeconds` fields.

- [ ] **Step 1: Update the `Beat` entity**

Edit `src/ProductionBible.Application/Entities/Beat.cs` to:

```csharp
namespace ProductionBible.Application.Entities;

public class Beat
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    public string SourceTimecode { get; set; } = "";
    public int Ordinal { get; set; }
    public int DurationSeconds { get; set; }
    public string Purpose { get; set; } = "";

    public List<AssetBeat> AssetBeats { get; set; } = new();
}
```

- [ ] **Step 2: Write the failing test for computed Start/End time**

`tests/ProductionBible.Application.Tests/BeatServiceTests.cs` already exists.
Append this new class to the **end of the file**, after the closing brace of
the existing `BeatServiceTests` class (same file, second top-level class —
this project already does this, e.g. `ImportMapperSqliteTests` sits in its
own file but `AssetServiceTests.cs` keeps everything in one class; either
convention is fine, a second class in the same file keeps this PR's new
Beat-ordering tests visually separate from the untouched CRUD tests above
them):

```csharp
public class BeatServiceOrderingTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    [Fact]
    public async Task GetByEpisodeAsync_computes_StartSeconds_and_EndSeconds_from_Ordinal_and_Duration()
    {
        await using var context = CreateInMemoryContext();
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        // Deliberately added out of Ordinal order, to prove sorting comes from
        // Ordinal, not from insertion order or from SourceTimecode text.
        var beatC = new Beat { Episode = episode, SourceTimecode = "20:00", Ordinal = 2, DurationSeconds = 60, Purpose = "Third" };
        var beatA = new Beat { Episode = episode, SourceTimecode = "00:00", Ordinal = 0, DurationSeconds = 40, Purpose = "First" };
        var beatB = new Beat { Episode = episode, SourceTimecode = "00:40", Ordinal = 1, DurationSeconds = 25, Purpose = "Second" };
        context.Beats.AddRange(beatC, beatA, beatB);
        await context.SaveChangesAsync();
        var service = new BeatService(context);

        var beats = await service.GetByEpisodeAsync(episode.Id);

        Assert.Equal(3, beats.Count);
        Assert.Equal("First", beats[0].Purpose);
        Assert.Equal(0, beats[0].StartSeconds);
        Assert.Equal(40, beats[0].EndSeconds);
        Assert.Equal("Second", beats[1].Purpose);
        Assert.Equal(40, beats[1].StartSeconds);
        Assert.Equal(65, beats[1].EndSeconds);
        Assert.Equal("Third", beats[2].Purpose);
        Assert.Equal(65, beats[2].StartSeconds);
        Assert.Equal(125, beats[2].EndSeconds);
    }

    [Fact]
    public async Task GetByEpisodeAsync_preserves_the_existing_Timecode_field_name_and_value()
    {
        await using var context = CreateInMemoryContext();
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var beat = new Beat { Episode = episode, SourceTimecode = "05:40", Ordinal = 0, DurationSeconds = 60, Purpose = "A beat" };
        context.Beats.Add(beat);
        await context.SaveChangesAsync();
        var service = new BeatService(context);

        var beats = await service.GetByEpisodeAsync(episode.Id);

        Assert.Equal("05:40", beats[0].Timecode);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~BeatServiceOrderingTests`
Expected: FAIL to compile — `Beat` has no `Ordinal`/`DurationSeconds`/`SourceTimecode`, `BeatDto` has no `StartSeconds`/`EndSeconds`.

- [ ] **Step 4: Update `BeatDto` and the request DTOs**

Edit `src/ProductionBible.Application/Dtos/BeatDtos.cs` to:

```csharp
namespace ProductionBible.Application.Dtos;

public record BeatDto(
    int Id,
    int EpisodeId,
    string Timecode,
    string Purpose,
    int Ordinal,
    int DurationSeconds,
    int StartSeconds,
    int EndSeconds,
    int[] AssetIds);

public record CreateBeatRequest(string Timecode, string Purpose, int Ordinal, int DurationSeconds);

public record UpdateBeatRequest(string Timecode, string Purpose, int Ordinal, int DurationSeconds);
```

- [ ] **Step 5: Update `BeatService` to populate the new fields and compute Start/End**

Replace the full contents of `src/ProductionBible.Application/Services/BeatService.cs` with:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class BeatService : IBeatService
{
    private readonly ProductionBibleDbContext _db;

    public BeatService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BeatDto>> GetByEpisodeAsync(int episodeId)
    {
        var beats = await _db.Beats
            .Include(b => b.AssetBeats)
            .Where(b => b.EpisodeId == episodeId)
            .OrderBy(b => b.Ordinal)
            .ToListAsync();

        var result = new List<BeatDto>();
        var cumulativeSeconds = 0;
        foreach (var beat in beats)
        {
            var startSeconds = cumulativeSeconds;
            var endSeconds = startSeconds + beat.DurationSeconds;
            cumulativeSeconds = endSeconds;

            result.Add(new BeatDto(
                beat.Id, beat.EpisodeId, beat.SourceTimecode, beat.Purpose,
                beat.Ordinal, beat.DurationSeconds, startSeconds, endSeconds,
                beat.AssetBeats
                    .OrderBy(ab => ab.OrderInBeat ?? int.MaxValue)
                    .Select(ab => ab.AssetId)
                    .ToArray()));
        }

        return result;
    }

    public async Task<BeatDto?> GetByIdAsync(int id)
    {
        var episodeId = await _db.Beats.Where(b => b.Id == id).Select(b => (int?)b.EpisodeId).SingleOrDefaultAsync();
        if (episodeId is null) return null;

        var beats = await GetByEpisodeAsync(episodeId.Value);
        return beats.SingleOrDefault(b => b.Id == id);
    }

    public async Task<BeatDto> CreateAsync(int episodeId, CreateBeatRequest request)
    {
        var beat = new Beat
        {
            EpisodeId = episodeId,
            SourceTimecode = request.Timecode,
            Purpose = request.Purpose,
            Ordinal = request.Ordinal,
            DurationSeconds = request.DurationSeconds,
        };
        _db.Beats.Add(beat);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(beat.Id))!;
    }

    public async Task<BeatDto?> UpdateAsync(int id, UpdateBeatRequest request)
    {
        var beat = await _db.Beats.FindAsync(id);
        if (beat is null) return null;

        beat.SourceTimecode = request.Timecode;
        beat.Purpose = request.Purpose;
        beat.Ordinal = request.Ordinal;
        beat.DurationSeconds = request.DurationSeconds;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var beat = await _db.Beats.FindAsync(id);
        if (beat is null) return false;

        _db.Beats.Remove(beat);
        await _db.SaveChangesAsync();
        return true;
    }
}
```

Note: `GetByIdAsync` now routes through `GetByEpisodeAsync` so its
`StartSeconds`/`EndSeconds` are computed against the beat's real neighbors in
Ordinal order, rather than being (incorrectly) always `0`/`DurationSeconds`
for a beat fetched in isolation. This also fixes the pre-existing
DTO-contract inconsistency the last plan's final review flagged (Minor #1:
`GetByIdAsync` didn't apply the same ordering as `GetByEpisodeAsync`) as a
side effect — call this out in your task report as a bonus fix, not a scope
violation.

- [ ] **Step 6: Update `ImportMapper`'s single `Beat` construction**

In `src/ProductionBible.Importer/ImportMapper.cs`, line 215, change:

```csharp
        var beat = new Beat { Episode = episode, Timecode = timecode, Purpose = purpose };
```

to:

```csharp
        var beat = new Beat { Episode = episode, SourceTimecode = timecode, Purpose = purpose };
```

Leave `Ordinal`/`DurationSeconds` unset (default `0`) here — Task 2 adds the
real computation as a separate pass. This step alone would make every
imported beat sort as Ordinal 0 (arbitrary order); that's expected and
temporary, corrected by Task 2 before this plan is done.

- [ ] **Step 7: Fix the three existing test files that construct `Beat` with `Timecode`**

In `tests/ProductionBible.Application.Tests/AssetServiceTests.cs`, in
`SeedAsync`, change:
```csharp
        var beat = new Beat { Episode = episode, Timecode = "00:00", Purpose = "Cold open" };
```
to:
```csharp
        var beat = new Beat { Episode = episode, SourceTimecode = "00:00", Purpose = "Cold open" };
```
And in the same file's `UpdateAsync_leaves_OrderInBeat_null_for_newly_added_beat_links` test:
```csharp
        var newBeat = new Beat { Episode = episode, Timecode = "01:00", Purpose = "New beat" };
```
to:
```csharp
        var newBeat = new Beat { Episode = episode, SourceTimecode = "01:00", Purpose = "New beat" };
```

In `tests/ProductionBible.Importer.Tests/ImportMapperTests.cs`, both
occurrences of `ab.Beat!.Timecode` (lines 48 and 82) become `ab.Beat!.SourceTimecode`,
and both occurrences of `b.Timecode == "16:30"`-style queries (line 98)
become `b.SourceTimecode == "16:30"`.

In `tests/ProductionBible.Importer.Tests/ImportMapperSqliteTests.cs`, both
`b.Timecode == "16:30"` / `b.Timecode == "09:00"` queries (lines 64 and 93)
become `b.SourceTimecode == "16:30"` / `b.SourceTimecode == "09:00"`.

**`tests/ProductionBible.Application.Tests/BeatServiceTests.cs` needs more
than a rename** — its existing `CreateBeatRequest(string, string)` call
sites break once `CreateBeatRequest` gains two required fields (Step 4
above), and two of its tests assert the exact behavior this task removes
(order derived from parsing `Timecode` text). Fix each:

Both direct `Beat` constructions — `Timecode = "00:00"` at line 67 (inside
`GetByIdAsync_includes_linked_asset_ids`) and line 136 (inside
`GetByEpisodeAsync_orders_assetIds_by_OrderInBeat_with_nulls_last`) — become
`SourceTimecode = "00:00"`. Neither test cares about `Ordinal`/
`DurationSeconds` (they test asset-linking, not beat ordering), so leave
those two properties at their default `0`.

Every remaining `CreateBeatRequest(timecode, purpose)` 2-arg call becomes a
4-arg call with an explicit `Ordinal`/`DurationSeconds` — the exact value
doesn't matter for these tests (none of them assert on `Ordinal` or
`DurationSeconds`), so use `0, 60` everywhere for consistency:

- Line 35 (`CreateAsync_then_GetByIdAsync_round_trips_the_beat`):
  `new CreateBeatRequest("00:00", "Cold open")` → `new CreateBeatRequest("00:00", "Cold open", 0, 60)`
- Lines 51-52 (`GetByEpisodeAsync_returns_beats_for_that_episode_only`):
  both `new CreateBeatRequest("00:00", "Cold open")` / `new CreateBeatRequest("00:00", "Different episode")` → append `, 0, 60` to each
- Line 88 (`DeleteAsync_removes_the_beat`):
  `new CreateBeatRequest("00:00", "Cold open")` → `new CreateBeatRequest("00:00", "Cold open", 0, 60)`

Finally, **delete these two tests entirely** —
`GetByEpisodeAsync_returns_beats_in_timecode_order_not_creation_order` (lines
96-111) and `GetByEpisodeAsync_sorts_unparseable_timecodes_last` (lines
113-127). Both assert that `GetByEpisodeAsync` derives order by parsing
`Timecode` text, which is exactly the behavior this task replaces —
`GetByEpisodeAsync` now sorts purely by `Ordinal` and never looks at
`SourceTimecode`'s content at all. Their coverage intent (order isn't
creation order; a beat's position doesn't depend on being text-parseable) is
already carried by the new
`GetByEpisodeAsync_computes_StartSeconds_and_EndSeconds_from_Ordinal_and_Duration`
test added in Step 2 above, which deliberately creates beats with
scrambled `SourceTimecode` text and out-of-insertion-order `Ordinal` values
to prove the sort key is `Ordinal` alone. Leave a one-line comment where the
two deleted tests were, noting why (e.g. `// Order-from-Timecode-text
behavior removed — see BeatServiceOrderingTests, which tests Ordinal-based
ordering instead.`).

- [ ] **Step 8: Add the EF migration**

Run, from the repo root:
```bash
dotnet ef migrations add RenameBeatTimecodeAddOrdinalAndDuration --project src/ProductionBible.Application --startup-project src/ProductionBible.Application
```

Read the generated migration file under `Migrations/` afterward and confirm
it does exactly three things: rename the `Timecode` column to
`SourceTimecode`, add `Ordinal` (`int`, `nullable: false`, no default needed
since the table is about to be dropped and reseeded), add `DurationSeconds`
(`int`, `nullable: false`). If EF generates a drop-and-recreate instead of a
rename (SQLite's migration provider sometimes does this for renames), that's
fine — the reseed step handles it either way — but confirm no other tables
are touched.

- [ ] **Step 9: Run the full test suite**

Run: `dotnet test`
Expected: PASS. (Some tests will still show beats sorted by their
default-`0` `Ordinal` from Step 6 — this is corrected by Task 2. Don't chase
that here; the failing-to-compile issues from Step 3 should now compile and
pass, since the new tests were written against post-Ordinal-computation
values that Task 2 will produce for the specific `dotnet ef`-independent unit
tests in Step 2 above, which set `Ordinal`/`DurationSeconds` directly and
don't depend on the importer at all.)

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Add Beat.Ordinal and DurationSeconds, rename Timecode to SourceTimecode

BeatDto's outward Timecode field is unchanged in name and value so no
Angular code needs to change. StartSeconds/EndSeconds are computed from
Ordinal+DurationSeconds rather than stored. GetByIdAsync now routes
through GetByEpisodeAsync's ordering, fixing a pre-existing DTO
inconsistency the last plan's final review flagged as a Minor.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 2: Importer computes Ordinal and DurationSeconds at import time

**Files:**
- Modify: `src/ProductionBible.Application/TimecodeOrdering.cs`
- Modify: `src/ProductionBible.Importer/ImportMapper.cs`
- Test: `tests/ProductionBible.Application.Tests/TimecodeOrderingTests.cs`
- Test: `tests/ProductionBible.Importer.Tests/ImportMapperTests.cs`

**Interfaces:**
- Consumes: `Beat.Ordinal`, `Beat.DurationSeconds`, `Beat.SourceTimecode` (Task 1). `TimecodeOrdering.TryParseSeconds` (unchanged).
- Produces: `TimecodeOrdering.AssignOrdinalsAndDurations(IReadOnlyList<Beat> beats)` — a new pure method, mutating each `Beat`'s `Ordinal`/`DurationSeconds` in place, given all the beats of one episode. `ImportMapper` calls it once per episode. Every `Beat` created by `ImportAsync` ends up with a correct `Ordinal` (0-based, per episode, by parsed-timecode order, unparseable last) and `DurationSeconds` (gap to next beat in that order, 60-second fallback for the last beat, or when the next beat is unparseable).

The real fixture data only has one page with an unparseable timecode ("All
five episodes", C-11) and it's the sole beat in its own synthetic episode —
not a useful case for proving "unparseable sorts last" against real data,
since there's nothing to sort it after. Extracting the algorithm out of
`ImportMapper` and into `TimecodeOrdering` (which already owns
`TryParseSeconds`) lets it be unit-tested directly against small synthetic
beat lists that DO exercise every branch, the same way `BeatServiceTests.cs`
already tests sort behavior against hand-built beats rather than only
through real fixture data.

- [ ] **Step 1: Write the failing unit tests for the extracted algorithm**

Add to `tests/ProductionBible.Application.Tests/TimecodeOrderingTests.cs`,
after the existing `TryParseSeconds` tests (needs `using
ProductionBible.Application.Entities;` added to the file's top, alongside
the implicit `ProductionBible.Application.Tests` namespace already there):

```csharp
public class TimecodeOrdering_AssignOrdinalsAndDurations_Tests
{
    [Fact]
    public void Assigns_Ordinal_and_gap_based_DurationSeconds_in_chronological_order()
    {
        var beatC = new Beat { SourceTimecode = "20:00", Purpose = "Third" };
        var beatA = new Beat { SourceTimecode = "00:00", Purpose = "First" };
        var beatB = new Beat { SourceTimecode = "00:40", Purpose = "Second" };
        var beats = new List<Beat> { beatC, beatA, beatB };

        TimecodeOrdering.AssignOrdinalsAndDurations(beats);

        Assert.Equal(0, beatA.Ordinal);
        Assert.Equal(40, beatA.DurationSeconds);
        Assert.Equal(1, beatB.Ordinal);
        Assert.Equal(1160, beatB.DurationSeconds); // 00:40 -> 20:00 is 1160 seconds
        Assert.Equal(2, beatC.Ordinal);
        Assert.Equal(60, beatC.DurationSeconds); // last beat, no next to gap against
    }

    [Fact]
    public void Sorts_an_unparseable_timecode_last_and_falls_back_to_60_seconds_for_its_neighbor()
    {
        var unscheduled = new Beat { SourceTimecode = "Reused in EP1 20:30 · EP3 12:00", Purpose = "Unscheduled" };
        var scheduled = new Beat { SourceTimecode = "00:00", Purpose = "Cold open" };
        var beats = new List<Beat> { unscheduled, scheduled };

        TimecodeOrdering.AssignOrdinalsAndDurations(beats);

        Assert.Equal(0, scheduled.Ordinal);
        // scheduled's "next" beat (unscheduled) can't be gapped against — 60s fallback, not a
        // huge or negative number from treating the unparseable text as seconds == 0.
        Assert.Equal(60, scheduled.DurationSeconds);
        Assert.Equal(1, unscheduled.Ordinal);
        Assert.Equal(60, unscheduled.DurationSeconds);
    }

    [Fact]
    public void A_single_beat_gets_Ordinal_zero_and_the_60_second_fallback()
    {
        var onlyBeat = new Beat { SourceTimecode = "16:30", Purpose = "Only beat in this episode" };
        var beats = new List<Beat> { onlyBeat };

        TimecodeOrdering.AssignOrdinalsAndDurations(beats);

        Assert.Equal(0, onlyBeat.Ordinal);
        Assert.Equal(60, onlyBeat.DurationSeconds);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~AssignOrdinalsAndDurations`
Expected: FAIL to compile — `TimecodeOrdering.AssignOrdinalsAndDurations` doesn't exist yet.

- [ ] **Step 3: Implement `AssignOrdinalsAndDurations` on `TimecodeOrdering`**

Edit `src/ProductionBible.Application/TimecodeOrdering.cs` to:

```csharp
using System.Text.RegularExpressions;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application;

public static class TimecodeOrdering
{
    public static bool TryParseSeconds(string timecode, out int seconds)
    {
        var match = Regex.Match(timecode, @"^(?:(?<h>\d+):)?(?<m>\d{1,2}):(?<s>\d{2})$");
        if (!match.Success)
        {
            seconds = 0;
            return false;
        }

        var hours = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value) : 0;
        var minutes = int.Parse(match.Groups["m"].Value);
        var secs = int.Parse(match.Groups["s"].Value);
        seconds = hours * 3600 + minutes * 60 + secs;
        return true;
    }

    /// <summary>
    /// Assigns Ordinal (0-based, sorted by parsed SourceTimecode, unparseable last) and
    /// DurationSeconds (gap to the next beat in that order; 60-second fallback for the last
    /// beat, or whenever the next beat's timecode isn't parseable) to every beat in the list.
    /// Mutates the beats in place. `beats` must already be scoped to a single episode.
    /// </summary>
    public static void AssignOrdinalsAndDurations(IReadOnlyList<Beat> beats)
    {
        var ordered = beats
            .Select(b => new
            {
                Beat = b,
                Parsed = TryParseSeconds(b.SourceTimecode, out var seconds),
                Seconds = seconds,
            })
            .OrderBy(x => x.Parsed ? 0 : 1)
            .ThenBy(x => x.Seconds)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Beat.Ordinal = i;

            var hasNext = i + 1 < ordered.Count;
            var gapIsMeaningful = hasNext && ordered[i].Parsed && ordered[i + 1].Parsed;
            ordered[i].Beat.DurationSeconds = gapIsMeaningful
                ? ordered[i + 1].Seconds - ordered[i].Seconds
                : 60;
        }
    }
}
```

This ports `BeatService`'s pre-existing `Parsed ? 0 : 1` / `ThenBy(Seconds)`
sort convention exactly, and `timeline-model.ts`'s existing gap-to-next-beat-
with-60s-fallback formula — now computed once here instead of on every
Timeline render.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~AssignOrdinalsAndDurations`
Expected: PASS.

- [ ] **Step 5: Write the failing integration test against real fixture data**

Add to `tests/ProductionBible.Importer.Tests/ImportMapperTests.cs`:

```csharp
    [Fact]
    public async Task Computes_Ordinal_and_DurationSeconds_for_EP1_beats_in_parsed_timecode_order()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var ep1Beats = await context.Beats
            .Include(b => b.Episode)
            .Where(b => b.Episode!.Name == "EP1")
            .OrderBy(b => b.Ordinal)
            .ToListAsync();

        // EP1's Coverage Check table gives these timecodes in order: 00:00, 00:38, 00:50,
        // 02:00, 04:00, 05:40, 07:20, 09:00, 11:00, 13:30, 16:30, 18:30, 20:30, 21:30, 22:20.
        var orderedSourceTimecodes = ep1Beats.Select(b => b.SourceTimecode).ToList();
        Assert.Equal(new List<string>
        {
            "00:00", "00:38", "00:50", "02:00", "04:00", "05:40", "07:20", "09:00",
            "11:00", "13:30", "16:30", "18:30", "20:30", "21:30", "22:20",
        }, orderedSourceTimecodes);

        // Ordinal is 0-based positional.
        Assert.Equal(Enumerable.Range(0, ep1Beats.Count).ToList(), ep1Beats.Select(b => b.Ordinal).ToList());

        // 00:00 -> 00:38 is a 38-second gap.
        Assert.Equal(38, ep1Beats[0].DurationSeconds);
        // 21:30 -> 22:20 is a 50-second gap.
        var beatAt2130 = ep1Beats.Single(b => b.SourceTimecode == "21:30");
        Assert.Equal(50, beatAt2130.DurationSeconds);
        // The last beat in the episode (22:20) falls back to 60 seconds — no next beat to gap against.
        var lastBeat = ep1Beats.Last();
        Assert.Equal("22:20", lastBeat.SourceTimecode);
        Assert.Equal(60, lastBeat.DurationSeconds);
    }
```

- [ ] **Step 6: Run the test to verify it fails**

Run: `dotnet test tests/ProductionBible.Importer.Tests --filter FullyQualifiedName~Computes_Ordinal_and_DurationSeconds`
Expected: FAIL — `ImportMapper` doesn't call `AssignOrdinalsAndDurations` yet, so every beat's `Ordinal` and `DurationSeconds` stay at their `0` defaults.

- [ ] **Step 7: Wire `AssignOrdinalsAndDurations` into `ImportMapper.ImportAsync`**

In `src/ProductionBible.Importer/ImportMapper.cs`, insert this block
immediately before the final `await _db.SaveChangesAsync();` (currently line
178, right after the Coverage Check `foreach (var beatEntry in
CoverageCheckParser.Parse(storyboardHtml))` loop closes):

```csharp
        foreach (var episodeGroup in _beatsByKey.GroupBy(kv => kv.Key.episodeNumber))
        {
            TimecodeOrdering.AssignOrdinalsAndDurations(episodeGroup.Select(kv => kv.Value).ToList());
        }

```

- [ ] **Step 8: Run the test to verify it passes**

Run: `dotnet test tests/ProductionBible.Importer.Tests --filter FullyQualifiedName~Computes_Ordinal_and_DurationSeconds`
Expected: PASS.

- [ ] **Step 9: Run the full test suite**

Run: `dotnet test`
Expected: PASS — all tests, including Task 1's.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Compute Beat.Ordinal and DurationSeconds at import time

TimecodeOrdering.AssignOrdinalsAndDurations ports timeline-model.ts's
existing gap-to-next-beat formula (60s fallback for the last beat,
unparseable timecodes sort last), extracted as its own testable method
rather than inlined in ImportMapper, since the real fixture data has no
episode that actually mixes a parseable and unparseable beat.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 3: Phase entity, service, controller, and DTOs

**Files:**
- Create: `src/ProductionBible.Application/Entities/Phase.cs`
- Modify: `src/ProductionBible.Application/Entities/Asset.cs`
- Modify: `src/ProductionBible.Application/Data/ProductionBibleDbContext.cs`
- Create: `src/ProductionBible.Application/Dtos/PhaseDtos.cs`
- Create: `src/ProductionBible.Application/Services/IPhaseService.cs`
- Create: `src/ProductionBible.Application/Services/PhaseService.cs`
- Create: `src/ProductionBible.Api/Controllers/PhasesController.cs`
- Modify: `src/ProductionBible.Api/Program.cs`
- Test: `tests/ProductionBible.Application.Tests/PhaseServiceTests.cs`

**Interfaces:**
- Consumes: `Project` entity (existing).
- Produces: `Phase` entity (`Id`, `ProjectId`, `Name`, `OrderIndex`), `Asset.PhaseId` (`int?`), `PhaseDto(int Id, int ProjectId, string Name, int OrderIndex)`, `IPhaseService` with the same five-method shape as `IEpisodeService` (`GetByProjectAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`).

- [ ] **Step 1: Create the `Phase` entity**

Create `src/ProductionBible.Application/Entities/Phase.cs`:

```csharp
namespace ProductionBible.Application.Entities;

public class Phase
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = "";
    public int OrderIndex { get; set; }

    public List<Asset> Assets { get; set; } = new();
}
```

- [ ] **Step 2: Add `PhaseId` to `Asset`**

Edit `src/ProductionBible.Application/Entities/Asset.cs`, adding after
`TargetLengthSeconds`:

```csharp
    public int? PhaseId { get; set; }
    public Phase? Phase { get; set; }
```

- [ ] **Step 3: Register `Phase` in the DbContext**

Edit `src/ProductionBible.Application/Data/ProductionBibleDbContext.cs`.
Add the `DbSet`:

```csharp
    public DbSet<Phase> Phases => Set<Phase>();
```

Add to `OnModelCreating`, alongside the other relationship configuration:

```csharp
        modelBuilder.Entity<Phase>()
            .HasOne(ph => ph.Project).WithMany().HasForeignKey(ph => ph.ProjectId);
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.Phase).WithMany(ph => ph.Assets).HasForeignKey(a => a.PhaseId);
```

- [ ] **Step 4: Write the failing test**

Create `tests/ProductionBible.Application.Tests/PhaseServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class PhaseServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    private static async Task<int> SeedProjectAsync(ProductionBibleDbContext context)
    {
        var project = new Project { Name = "HalfNut ELS" };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project.Id;
    }

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_round_trips_the_phase()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new PhaseService(context);

        var created = await service.CreateAsync(projectId, new CreatePhaseRequest("Phase 1: Setup A", 0));

        Assert.True(created.Id > 0);
        Assert.Equal(projectId, created.ProjectId);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Phase 1: Setup A", fetched!.Name);
        Assert.Equal(0, fetched.OrderIndex);
    }

    [Fact]
    public async Task GetByProjectAsync_returns_only_that_projects_phases_in_order()
    {
        await using var context = CreateInMemoryContext();
        var projectAId = await SeedProjectAsync(context);
        var projectBId = await SeedProjectAsync(context);
        var service = new PhaseService(context);
        await service.CreateAsync(projectAId, new CreatePhaseRequest("Phase 2: Setup C", 1));
        await service.CreateAsync(projectAId, new CreatePhaseRequest("Phase 1: Setup A", 0));
        await service.CreateAsync(projectBId, new CreatePhaseRequest("Other Project Phase 1", 0));

        var phases = await service.GetByProjectAsync(projectAId);

        Assert.Equal(2, phases.Count);
        Assert.Equal("Phase 1: Setup A", phases[0].Name);
        Assert.Equal("Phase 2: Setup C", phases[1].Name);
    }

    [Fact]
    public async Task UpdateAsync_changes_name_and_order()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new PhaseService(context);
        var created = await service.CreateAsync(projectId, new CreatePhaseRequest("Draft Name", 0));

        var updated = await service.UpdateAsync(created.Id, new UpdatePhaseRequest("Phase 1: Setup A", 0));

        Assert.NotNull(updated);
        Assert.Equal("Phase 1: Setup A", updated!.Name);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_phase()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new PhaseService(context);
        var created = await service.CreateAsync(projectId, new CreatePhaseRequest("Phase 1: Setup A", 0));

        var deleted = await service.DeleteAsync(created.Id);
        var fetched = await service.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(fetched);
    }
}
```

- [ ] **Step 5: Run the test to verify it fails**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~PhaseServiceTests`
Expected: FAIL to compile — `PhaseService`, `CreatePhaseRequest`, `UpdatePhaseRequest` don't exist yet.

- [ ] **Step 6: Create the DTOs**

Create `src/ProductionBible.Application/Dtos/PhaseDtos.cs`:

```csharp
namespace ProductionBible.Application.Dtos;

public record PhaseDto(int Id, int ProjectId, string Name, int OrderIndex);

public record CreatePhaseRequest(string Name, int OrderIndex);

public record UpdatePhaseRequest(string Name, int OrderIndex);
```

- [ ] **Step 7: Create `IPhaseService`**

Create `src/ProductionBible.Application/Services/IPhaseService.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IPhaseService
{
    Task<IReadOnlyList<PhaseDto>> GetByProjectAsync(int projectId);
    Task<PhaseDto?> GetByIdAsync(int id);
    Task<PhaseDto> CreateAsync(int projectId, CreatePhaseRequest request);
    Task<PhaseDto?> UpdateAsync(int id, UpdatePhaseRequest request);
    Task<bool> DeleteAsync(int id);
}
```

- [ ] **Step 8: Create `PhaseService`**

Create `src/ProductionBible.Application/Services/PhaseService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class PhaseService : IPhaseService
{
    private readonly ProductionBibleDbContext _db;

    public PhaseService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    private static PhaseDto ToDto(Phase p) => new(p.Id, p.ProjectId, p.Name, p.OrderIndex);

    public async Task<IReadOnlyList<PhaseDto>> GetByProjectAsync(int projectId)
    {
        return await _db.Phases
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.OrderIndex)
            .Select(p => new PhaseDto(p.Id, p.ProjectId, p.Name, p.OrderIndex))
            .ToListAsync();
    }

    public async Task<PhaseDto?> GetByIdAsync(int id)
    {
        var phase = await _db.Phases.FindAsync(id);
        return phase is null ? null : ToDto(phase);
    }

    public async Task<PhaseDto> CreateAsync(int projectId, CreatePhaseRequest request)
    {
        var phase = new Phase { ProjectId = projectId, Name = request.Name, OrderIndex = request.OrderIndex };
        _db.Phases.Add(phase);
        await _db.SaveChangesAsync();
        return ToDto(phase);
    }

    public async Task<PhaseDto?> UpdateAsync(int id, UpdatePhaseRequest request)
    {
        var phase = await _db.Phases.FindAsync(id);
        if (phase is null) return null;

        phase.Name = request.Name;
        phase.OrderIndex = request.OrderIndex;
        await _db.SaveChangesAsync();
        return ToDto(phase);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var phase = await _db.Phases.FindAsync(id);
        if (phase is null) return false;

        _db.Phases.Remove(phase);
        await _db.SaveChangesAsync();
        return true;
    }
}
```

- [ ] **Step 9: Run the test to verify it passes**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~PhaseServiceTests`
Expected: PASS.

- [ ] **Step 10: Create `PhasesController`**

Create `src/ProductionBible.Api/Controllers/PhasesController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
public class PhasesController : ControllerBase
{
    private readonly IPhaseService _service;

    public PhasesController(IPhaseService service)
    {
        _service = service;
    }

    [HttpGet("api/projects/{projectId:int}/phases")]
    public async Task<ActionResult<IReadOnlyList<PhaseDto>>> GetByProject(int projectId)
        => Ok(await _service.GetByProjectAsync(projectId));

    [HttpGet("api/phases/{id:int}")]
    public async Task<ActionResult<PhaseDto>> GetById(int id)
    {
        var phase = await _service.GetByIdAsync(id);
        return phase is null ? NotFound() : Ok(phase);
    }

    [HttpPost("api/projects/{projectId:int}/phases")]
    public async Task<ActionResult<PhaseDto>> Create(int projectId, CreatePhaseRequest request)
    {
        var created = await _service.CreateAsync(projectId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("api/phases/{id:int}")]
    public async Task<ActionResult<PhaseDto>> Update(int id, UpdatePhaseRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("api/phases/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

- [ ] **Step 11: Register `IPhaseService` in `Program.cs`**

In `src/ProductionBible.Api/Program.cs`, add after the existing
`AddScoped<IBeatService, BeatService>();` line:

```csharp
builder.Services.AddScoped<IPhaseService, PhaseService>();
```

- [ ] **Step 12: Add the EF migration**

Run, from the repo root:
```bash
dotnet ef migrations add AddPhaseEntityAndAssetPhaseId --project src/ProductionBible.Application --startup-project src/ProductionBible.Application
```

Read the generated migration and confirm it creates one `Phases` table
(`Id`, `ProjectId`, `Name`, `OrderIndex`) with a FK to `Projects`, and adds
one nullable `PhaseId` column + FK to `Assets`. Nothing else should change.

- [ ] **Step 13: Run the full test suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 14: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Add Phase as a first-class entity with CRUD

Phase gets its own table (scoped to Project, matching Episode's shape)
and Asset.PhaseId FK, replacing the plan to keep phases as an
Attributes["PhaseGroup"] string. PhasesController/PhaseService mirror
EpisodesController/EpisodeService exactly.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 4: Importer creates real Phase rows instead of the PhaseGroup attribute

**Files:**
- Modify: `src/ProductionBible.Importer/ImportMapper.cs`
- Test: `tests/ProductionBible.Importer.Tests/ImportMapperTests.cs`

**Interfaces:**
- Consumes: `Phase` entity, `Asset.PhaseId` (Task 3).
- Produces: every shot page's `PhaseGroup` string becomes a real `Phase` row (deduplicated per `(ProjectId, Name)`), with `Asset.PhaseId` set. The importer stops calling `AddAttribute(asset, "PhaseGroup", page.PhaseGroup)`.

- [ ] **Step 1: Write the failing test**

Add to `tests/ProductionBible.Importer.Tests/ImportMapperTests.cs`:

```csharp
    [Fact]
    public async Task Shot_pages_get_a_real_Phase_row_instead_of_a_PhaseGroup_attribute()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var a01 = await context.Assets
            .Include(a => a.Attributes)
            .Include(a => a.Phase)
            .SingleAsync(a => a.Code == "A-01");

        Assert.NotNull(a01.Phase);
        Assert.DoesNotContain(a01.Attributes, attr => attr.Key == "PhaseGroup");

        // Two production_plan.md pages that share a phase (both "Setup A" shots, per the
        // fixture) resolve to the SAME Phase row, not two separate rows with the same name.
        var a02 = await context.Assets.Include(a => a.Phase).SingleAsync(a => a.Code == "A-02");
        Assert.Equal(a01.PhaseId, a02.PhaseId);

        var phaseCount = await context.Phases.CountAsync();
        Assert.True(phaseCount > 0);
    }
```

Confirmed against the real `production_plan.md`: A-01 and A-02 both carry
`"Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in)"` as their
`PhaseGroup` value — the same pair used in this test is a real, verified
match, not a guess. If the fixture copy under
`tests/ProductionBible.Importer.Tests/Fixtures/production_plan.md` has
diverged from the source repo's copy, `dotnet test` will simply fail this
assertion, which is itself useful signal.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/ProductionBible.Importer.Tests --filter FullyQualifiedName~Shot_pages_get_a_real_Phase_row`
Expected: FAIL — `a01.Phase` is null, `a01.Attributes` still contains `PhaseGroup`.

- [ ] **Step 3: Add `GetOrCreatePhase` and wire it into the shot-page loop**

In `src/ProductionBible.Importer/ImportMapper.cs`, add a new dictionary field
alongside the existing ones near the top of the class:

```csharp
    private readonly Dictionary<(int projectId, string name), Phase> _phasesByKey = new();
```

Add a new private method, near `GetOrCreateEpisode`:

```csharp
    private Phase GetOrCreatePhase(Project project, string name)
    {
        var key = (project.Id, name);
        if (_phasesByKey.TryGetValue(key, out var existing)) return existing;
        var phase = new Phase { Project = project, Name = name, OrderIndex = _phasesByKey.Count };
        _phasesByKey[key] = phase;
        _db.Phases.Add(phase);
        return phase;
    }
```

`project.Id` is `0` until `SaveChangesAsync` runs, same as every other
not-yet-saved entity in this importer — but that's fine here: the key only
needs to distinguish phases *within a single `ImportAsync` call*, where
there's exactly one `Project` object throughout, so `project.Id` being `0`
for the whole run doesn't create any collision. (This mirrors the same
reasoning that made the earlier `ab.Asset == shotAsset` reference-equality
fix correct — identity within one in-memory run, not database identity,
is what these dictionaries need.)

In the shot-page loop, replace:

```csharp
            AddAttribute(asset, "PhaseGroup", page.PhaseGroup);
```

with:

```csharp
            asset.Phase = GetOrCreatePhase(project, page.PhaseGroup);
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/ProductionBible.Importer.Tests --filter FullyQualifiedName~Shot_pages_get_a_real_Phase_row`
Expected: PASS.

- [ ] **Step 5: Run the full test suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Importer creates real Phase rows instead of a PhaseGroup attribute

GetOrCreatePhase follows the same get-or-create-by-key pattern as
GetOrCreateEpisode. Asset.Phase is set directly; the importer no longer
writes Attributes["PhaseGroup"].

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 5: Reorder endpoints for beats, phases, phase-assets, and asset-beats

**Files:**
- Modify: `src/ProductionBible.Application/Services/IBeatService.cs`
- Modify: `src/ProductionBible.Application/Services/BeatService.cs`
- Modify: `src/ProductionBible.Application/Services/IPhaseService.cs`
- Modify: `src/ProductionBible.Application/Services/PhaseService.cs`
- Modify: `src/ProductionBible.Application/Services/IAssetService.cs`
- Modify: `src/ProductionBible.Application/Services/AssetService.cs`
- Modify: `src/ProductionBible.Api/Controllers/BeatsController.cs`
- Modify: `src/ProductionBible.Api/Controllers/PhasesController.cs`
- Modify: `src/ProductionBible.Api/Controllers/AssetsController.cs`
- Create: `src/ProductionBible.Application/Dtos/ReorderRequest.cs`
- Test: `tests/ProductionBible.Application.Tests/BeatServiceTests.cs` (or `BeatServiceOrderingTests` from Task 1)
- Test: `tests/ProductionBible.Application.Tests/PhaseServiceTests.cs`
- Test: `tests/ProductionBible.Application.Tests/AssetServiceTests.cs`

**Interfaces:**
- Consumes: `Beat.Ordinal`, `Phase.OrderIndex`, `Asset.SequenceNumber`, `AssetBeat.OrderInBeat` (all pre-existing after Tasks 1-3).
- Produces:
  - `IBeatService.ReorderAsync(int episodeId, int[] orderedBeatIds) : Task<bool>`
  - `IPhaseService.ReorderAsync(int projectId, int[] orderedPhaseIds) : Task<bool>`
  - `IAssetService.ReorderWithinPhaseAsync(int phaseId, int[] orderedAssetIds) : Task<bool>`
  - `IAssetService.ReorderWithinBeatAsync(int beatId, int[] orderedAssetIds) : Task<bool>`
  - Each returns `false` if any ID in the array doesn't belong to the named parent (nothing is written in that case); `true` on success.
  - `record ReorderRequest(int[] OrderedIds);` — shared request body shape for all four endpoints.

- [ ] **Step 1: Create the shared `ReorderRequest` DTO**

Create `src/ProductionBible.Application/Dtos/ReorderRequest.cs`:

```csharp
namespace ProductionBible.Application.Dtos;

public record ReorderRequest(int[] OrderedIds);
```

- [ ] **Step 2: Write the failing test for beat reorder**

Add to `tests/ProductionBible.Application.Tests/BeatServiceTests.cs` (the
file from Task 1 — add alongside `BeatServiceOrderingTests`, in the same
class or a new one in the same file; either is fine):

```csharp
    [Fact]
    public async Task ReorderAsync_reassigns_Ordinal_to_match_the_given_order()
    {
        await using var context = CreateInMemoryContext();
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var beatA = new Beat { Episode = episode, SourceTimecode = "00:00", Ordinal = 0, DurationSeconds = 60, Purpose = "A" };
        var beatB = new Beat { Episode = episode, SourceTimecode = "01:00", Ordinal = 1, DurationSeconds = 60, Purpose = "B" };
        context.Beats.AddRange(beatA, beatB);
        await context.SaveChangesAsync();
        var service = new BeatService(context);

        var result = await service.ReorderAsync(episode.Id, new[] { beatB.Id, beatA.Id });

        Assert.True(result);
        var reordered = await service.GetByEpisodeAsync(episode.Id);
        Assert.Equal("B", reordered[0].Purpose);
        Assert.Equal(0, reordered[0].Ordinal);
        Assert.Equal("A", reordered[1].Purpose);
        Assert.Equal(1, reordered[1].Ordinal);
    }

    [Fact]
    public async Task ReorderAsync_rejects_an_id_that_does_not_belong_to_the_episode_and_writes_nothing()
    {
        await using var context = CreateInMemoryContext();
        var project = new Project { Name = "HalfNut ELS" };
        var episodeA = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var episodeB = new Episode { Project = project, Name = "EP2", OrderIndex = 2 };
        var beatA = new Beat { Episode = episodeA, SourceTimecode = "00:00", Ordinal = 0, DurationSeconds = 60, Purpose = "A" };
        var beatFromOtherEpisode = new Beat { Episode = episodeB, SourceTimecode = "00:00", Ordinal = 0, DurationSeconds = 60, Purpose = "Other" };
        context.Beats.AddRange(beatA, beatFromOtherEpisode);
        await context.SaveChangesAsync();
        var service = new BeatService(context);

        var result = await service.ReorderAsync(episodeA.Id, new[] { beatFromOtherEpisode.Id, beatA.Id });

        Assert.False(result);
        var unchanged = await service.GetByEpisodeAsync(episodeA.Id);
        Assert.Equal(0, unchanged.Single().Ordinal);
    }
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~ReorderAsync`
Expected: FAIL to compile — `IBeatService`/`BeatService` have no `ReorderAsync`.

- [ ] **Step 4: Add `ReorderAsync` to `IBeatService` and `BeatService`**

In `src/ProductionBible.Application/Services/IBeatService.cs`, add:

```csharp
    Task<bool> ReorderAsync(int episodeId, int[] orderedBeatIds);
```

In `src/ProductionBible.Application/Services/BeatService.cs`, add:

```csharp
    public async Task<bool> ReorderAsync(int episodeId, int[] orderedBeatIds)
    {
        var beats = await _db.Beats.Where(b => b.EpisodeId == episodeId).ToListAsync();
        if (beats.Count != orderedBeatIds.Length) return false;

        var beatsById = beats.ToDictionary(b => b.Id);
        if (orderedBeatIds.Any(id => !beatsById.ContainsKey(id))) return false;

        for (var i = 0; i < orderedBeatIds.Length; i++)
        {
            beatsById[orderedBeatIds[i]].Ordinal = i;
        }

        await _db.SaveChangesAsync();
        return true;
    }
```

- [ ] **Step 5: Run the beat reorder tests to verify they pass**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~ReorderAsync`
Expected: PASS for the beat tests (the phase/asset ones from later steps don't exist yet — that's fine).

- [ ] **Step 6: Write the failing test for phase reorder**

Add to `tests/ProductionBible.Application.Tests/PhaseServiceTests.cs`:

```csharp
    [Fact]
    public async Task ReorderAsync_reassigns_OrderIndex_to_match_the_given_order()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new PhaseService(context);
        var phaseA = await service.CreateAsync(projectId, new CreatePhaseRequest("Setup A", 0));
        var phaseC = await service.CreateAsync(projectId, new CreatePhaseRequest("Setup C", 1));

        var result = await service.ReorderAsync(projectId, new[] { phaseC.Id, phaseA.Id });

        Assert.True(result);
        var reordered = await service.GetByProjectAsync(projectId);
        Assert.Equal("Setup C", reordered[0].Name);
        Assert.Equal("Setup A", reordered[1].Name);
    }
```

- [ ] **Step 7: Add `ReorderAsync` to `IPhaseService` and `PhaseService`**

In `src/ProductionBible.Application/Services/IPhaseService.cs`, add:

```csharp
    Task<bool> ReorderAsync(int projectId, int[] orderedPhaseIds);
```

In `src/ProductionBible.Application/Services/PhaseService.cs`, add:

```csharp
    public async Task<bool> ReorderAsync(int projectId, int[] orderedPhaseIds)
    {
        var phases = await _db.Phases.Where(p => p.ProjectId == projectId).ToListAsync();
        if (phases.Count != orderedPhaseIds.Length) return false;

        var phasesById = phases.ToDictionary(p => p.Id);
        if (orderedPhaseIds.Any(id => !phasesById.ContainsKey(id))) return false;

        for (var i = 0; i < orderedPhaseIds.Length; i++)
        {
            phasesById[orderedPhaseIds[i]].OrderIndex = i;
        }

        await _db.SaveChangesAsync();
        return true;
    }
```

- [ ] **Step 8: Run the phase reorder test to verify it passes**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~PhaseServiceTests`
Expected: PASS.

- [ ] **Step 9: Write the failing tests for asset reorder (within a phase, and within a beat)**

Add to `tests/ProductionBible.Application.Tests/AssetServiceTests.cs`:

```csharp
    [Fact]
    public async Task ReorderWithinPhaseAsync_reassigns_SequenceNumber_to_match_the_given_order()
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
        Assert.Equal(0, reorderedB!.SequenceNumber);
        Assert.Equal(1, reorderedA!.SequenceNumber);
    }

    [Fact]
    public async Task ReorderWithinBeatAsync_reassigns_OrderInBeat_to_match_the_given_order()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);
        var assetA = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "A", null, "Planned", null, null, null, null, new[] { beatId }));
        var assetB = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-02", "B", null, "Planned", null, null, null, null, new[] { beatId }));

        var result = await service.ReorderWithinBeatAsync(beatId, new[] { assetB.Id, assetA.Id });

        Assert.True(result);
        var orderInBeatForB = await context.AssetBeats.AsNoTracking()
            .Where(ab => ab.BeatId == beatId && ab.AssetId == assetB.Id).Select(ab => ab.OrderInBeat).SingleAsync();
        var orderInBeatForA = await context.AssetBeats.AsNoTracking()
            .Where(ab => ab.BeatId == beatId && ab.AssetId == assetA.Id).Select(ab => ab.OrderInBeat).SingleAsync();
        Assert.Equal(0, orderInBeatForB);
        Assert.Equal(1, orderInBeatForA);
    }
```

- [ ] **Step 10: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~ReorderWithin`
Expected: FAIL to compile — `IAssetService`/`AssetService` have no
`ReorderWithinPhaseAsync`/`ReorderWithinBeatAsync`.

- [ ] **Step 11: Add both reorder methods to `IAssetService` and `AssetService`**

In `src/ProductionBible.Application/Services/IAssetService.cs`, add:

```csharp
    Task<bool> ReorderWithinPhaseAsync(int phaseId, int[] orderedAssetIds);
    Task<bool> ReorderWithinBeatAsync(int beatId, int[] orderedAssetIds);
```

In `src/ProductionBible.Application/Services/AssetService.cs`, add:

```csharp
    public async Task<bool> ReorderWithinPhaseAsync(int phaseId, int[] orderedAssetIds)
    {
        var assets = await _db.Assets.Where(a => a.PhaseId == phaseId).ToListAsync();
        if (assets.Count != orderedAssetIds.Length) return false;

        var assetsById = assets.ToDictionary(a => a.Id);
        if (orderedAssetIds.Any(id => !assetsById.ContainsKey(id))) return false;

        for (var i = 0; i < orderedAssetIds.Length; i++)
        {
            assetsById[orderedAssetIds[i]].SequenceNumber = i;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderWithinBeatAsync(int beatId, int[] orderedAssetIds)
    {
        var assetBeats = await _db.AssetBeats.Where(ab => ab.BeatId == beatId).ToListAsync();
        if (assetBeats.Count != orderedAssetIds.Length) return false;

        var assetBeatsByAssetId = assetBeats.ToDictionary(ab => ab.AssetId);
        if (orderedAssetIds.Any(id => !assetBeatsByAssetId.ContainsKey(id))) return false;

        for (var i = 0; i < orderedAssetIds.Length; i++)
        {
            assetBeatsByAssetId[orderedAssetIds[i]].OrderInBeat = i;
        }

        await _db.SaveChangesAsync();
        return true;
    }
```

- [ ] **Step 12: Run the tests to verify they pass**

Run: `dotnet test tests/ProductionBible.Application.Tests --filter FullyQualifiedName~ReorderWithin`
Expected: PASS.

- [ ] **Step 13: Wire up the four controller endpoints**

In `src/ProductionBible.Api/Controllers/BeatsController.cs`, add:

```csharp
    [HttpPatch("api/episodes/{episodeId:int}/beats/reorder")]
    public async Task<IActionResult> Reorder(int episodeId, ReorderRequest request)
    {
        var succeeded = await _service.ReorderAsync(episodeId, request.OrderedIds);
        return succeeded ? NoContent() : BadRequest();
    }
```

In `src/ProductionBible.Api/Controllers/PhasesController.cs`, add:

```csharp
    [HttpPatch("api/projects/{projectId:int}/phases/reorder")]
    public async Task<IActionResult> Reorder(int projectId, ReorderRequest request)
    {
        var succeeded = await _service.ReorderAsync(projectId, request.OrderedIds);
        return succeeded ? NoContent() : BadRequest();
    }
```

In `src/ProductionBible.Api/Controllers/AssetsController.cs`, add:

```csharp
    [HttpPatch("api/phases/{phaseId:int}/assets/reorder")]
    public async Task<IActionResult> ReorderWithinPhase(int phaseId, ReorderRequest request)
    {
        var succeeded = await _service.ReorderWithinPhaseAsync(phaseId, request.OrderedIds);
        return succeeded ? NoContent() : BadRequest();
    }

    [HttpPatch("api/beats/{beatId:int}/asset-beats/reorder")]
    public async Task<IActionResult> ReorderWithinBeat(int beatId, ReorderRequest request)
    {
        var succeeded = await _service.ReorderWithinBeatAsync(beatId, request.OrderedIds);
        return succeeded ? NoContent() : BadRequest();
    }
```

- [ ] **Step 14: Write one integration test proving a real multi-row reorder commits atomically**

This project has already been bitten once (the Critical `ImportMapper` ID-
comparison bug) by a multi-row EF Core operation that worked against
InMemory but silently corrupted data against real SQLite. Reorder endpoints
are the same shape of risk — mutating several rows in one
`SaveChangesAsync` — so prove at least one of them against real SQLite.

Create `tests/ProductionBible.Api.Tests/BeatReorderSqliteTests.cs`, matching
the existing `ProjectsApiTests.cs`'s `IClassFixture<TestWebApplicationFactory>`
pattern exactly (that factory already runs against a real temp-file SQLite
database, not InMemory — this test rides on that for free):

```csharp
using System.Net.Http.Json;
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests;

public class BeatReorderSqliteTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BeatReorderSqliteTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reordering_two_beats_through_the_real_http_pipeline_persists_the_new_order()
    {
        var client = _factory.CreateClient();

        var project = await (await client.PostAsJsonAsync(
            "/api/projects", new CreateProjectRequest("HalfNut ELS", null))).Content.ReadFromJsonAsync<ProjectDto>();
        var episode = await (await client.PostAsJsonAsync(
            $"/api/projects/{project!.Id}/episodes", new CreateEpisodeRequest("EP1", 1))).Content.ReadFromJsonAsync<EpisodeDto>();
        var beatA = await (await client.PostAsJsonAsync(
            $"/api/episodes/{episode!.Id}/beats", new CreateBeatRequest("00:00", "A", 0, 60))).Content.ReadFromJsonAsync<BeatDto>();
        var beatB = await (await client.PostAsJsonAsync(
            $"/api/episodes/{episode.Id}/beats", new CreateBeatRequest("01:00", "B", 1, 60))).Content.ReadFromJsonAsync<BeatDto>();

        var reorderResponse = await client.PatchAsJsonAsync(
            $"/api/episodes/{episode.Id}/beats/reorder", new ReorderRequest(new[] { beatB!.Id, beatA!.Id }));
        reorderResponse.EnsureSuccessStatusCode();

        var beats = await (await client.GetAsync($"/api/episodes/{episode.Id}/beats"))
            .Content.ReadFromJsonAsync<List<BeatDto>>();

        Assert.Equal("B", beats![0].Purpose);
        Assert.Equal("A", beats[1].Purpose);
    }
}
```

- [ ] **Step 15: Run the full test suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 16: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Add reorder endpoints for beats, phases, phase-assets, and asset-beats

Each endpoint takes an ordered ID array and reassigns the relevant
ordinal field (Beat.Ordinal, Phase.OrderIndex, Asset.SequenceNumber,
AssetBeat.OrderInBeat) in one SaveChangesAsync. Any ID that doesn't
belong to the named parent rejects the whole request with no writes.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 6: Reseed and live verification

**Files:** none (no code changes — this task is a verification pass, per this project's established convention of a mandatory live-check step at the end of a plan, which caught a Critical bug in the previous one).

**Interfaces:** none new — this task only exercises Tasks 1-5's endpoints against a freshly reseeded real database.

- [ ] **Step 1: Delete the existing database**

```bash
rm -f src/ProductionBible.Api/App_Data/productionbible.db src/ProductionBible.Api/App_Data/productionbible.db-shm src/ProductionBible.Api/App_Data/productionbible.db-wal
```

- [ ] **Step 2: Run the app once to apply all migrations**

```bash
dotnet run --project src/ProductionBible.Api &
sleep 5
kill %1
```

(Or start it and stop it manually if running interactively — the point is
just to let `db.Database.Migrate()` in `Program.cs` create the fresh schema.)

- [ ] **Step 3: Reseed from the real source documents**

```bash
dotnet run --project src/ProductionBible.Importer -- \
  "D:\Data\source\HalfNutELS-Video\storyboard.html" \
  "D:\Data\source\HalfNutELS-Video\production_plan.md" \
  src/ProductionBible.Api/App_Data/productionbible.db
```

Confirmed against the current `src/ProductionBible.Importer/Program.cs`:
argument order is `<storyboard.html path> <production_plan.md path> <sqlite
db path>`, exactly as used above.

- [ ] **Step 4: Start the app and hit the new endpoints directly**

```bash
dotnet run --project src/ProductionBible.Api &
sleep 5
curl -s http://localhost:5280/api/projects | head -c 500
```

Get a real project ID from that response, then:

```bash
curl -s "http://localhost:5280/api/projects/<projectId>/episodes"
```

Get a real EP1 episode ID from that response, then:

```bash
curl -s "http://localhost:5280/api/episodes/<episodeId>/beats" | head -c 2000
```

**Verify by eye:**
- Each beat has non-zero-looking `ordinal`/`durationSeconds`/`startSeconds`/`endSeconds` fields (not all `0`), and `timecode` still reads like `"00:00"`, `"05:40"`, etc. — the same values it always did.
- Beats come back in ascending `startSeconds`/`ordinal` order.

```bash
curl -s "http://localhost:5280/api/projects/<projectId>/phases" | head -c 2000
```

**Verify by eye:** several `Phase` rows exist with names like `"Phase 1:
Setup A"`, not one giant "PhaseGroup" attribute anymore.

```bash
curl -s "http://localhost:5280/api/episodes/<episodeId>/assets" | head -c 2000
```

**Verify by eye:** assets carry a non-null `phaseId` for shot/PTC assets
(Animation/Title assets are expected to have `phaseId: null` — they were
never in a shoot-day phase).

- [ ] **Step 5: Verify the interim Production Plan regression is graceful, not a crash**

Open `http://localhost:5280/production-plan` in a browser (or `curl` the
page and confirm no 500). Per the spec's documented interim consequence:
every asset's `Attributes["PhaseGroup"]` is now absent, so
`phase-grouping.ts` groups everything under one "Unphased" heading. Confirm
that's what actually renders — a single "Unphased" group with all assets in
it, not an error, a blank page, or a JS exception in the browser console.

- [ ] **Step 6: Stop the app**

```bash
kill %1
```

- [ ] **Step 7: Report findings**

Write up what you verified (or any discrepancy found) in your task report.
If anything in Steps 4-5 doesn't match what's described, that's a real bug
in Tasks 1-5 — do not silently note it and move on; flag it clearly as a
concern in your report so the controller can decide whether to fix it before
this plan is considered done, per this project's established pattern (the
previous plan's Task 8 caught its Critical bug at exactly this kind of
step).

- [ ] **Step 8: Commit the reseeded database is NOT committed**

`App_Data/*.db*` should already be gitignored (confirm with `git status` —
it should show no changes from the reseed). If it's not gitignored, stop and
flag this as a concern rather than committing a binary database file.
