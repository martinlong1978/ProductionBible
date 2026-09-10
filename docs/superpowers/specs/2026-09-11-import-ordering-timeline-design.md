# Import Correctness, Beat/Asset Ordering, and Timeline Redesign — Design Spec

## Overview

This spec covers four related GitHub issues, tackled together because they share data and
form a dependency chain:

- **[#1](https://github.com/martinlong1978/ProductionBible/issues/1)** — Animation/Title
  assets are never linked to any `Beat` by the importer.
- **[#2](https://github.com/martinlong1978/ProductionBible/issues/2)** — Beats display in
  import order, not storyboard/episode timecode order.
- **[#11](https://github.com/martinlong1978/ProductionBible/issues/11)** — Asset ordering
  within a beat uses shooting order, not storyboard/narrative order; needs a distinct field.
- **[#12](https://github.com/martinlong1978/ProductionBible/issues/12)** — Redesign the
  Timeline view as a vertical, DaVinci-Resolve-style timeline (already carries its own
  detailed design from that issue's body; this spec's Timeline section only adds the
  integration points with the new `OrderInBeat` field, and defers to the issue for everything
  else).

**Order:** #1 and #2 are independent fixes. #11 depends on #1 (both consume the same new
"Coverage check" table parser). #12 depends on all three being correct first — a timeline
built on wrong ordering or missing links would look authoritative while being wrong, per that
issue's own explicit blocking note.

## Background: the "Coverage check" table

`storyboard.html` (in `D:\Data\source\HalfNutELS-Video`, this app's import source) has a table
neither the original importer nor Phase 1's plan used:

```html
<h3>Coverage check</h3>
<p>All 66 beats in the five scripts, against what covers them. Four need no capture at all.</p>
<table>
  <thead><tr><th>Episode</th><th>Beat &rarr; coverage</th></tr></thead>
  <tbody>
    <tr><td><strong>EP 1</strong></td><td><code>00:00</code> A-01/02/03, B-01 &middot; <code>00:38</code> VO over the above &middot; <code>00:50</code> E-L &middot; <code>02:00</code> B-05 + <strong>G1</strong> &middot; <code>04:00</code> B-02, B-06 &middot; <code>05:40</code> B-03, B-04, E-L &middot; <code>07:20</code> <strong>G1</strong> &middot; <code>09:00</code> <strong>f1</strong>, E-B &middot; <code>11:00</code> <strong>f3</strong>, <strong>f5</strong>, E-B &middot; <code>13:30</code> C-12, E-B &middot; <code>16:30</code> <strong>G5</strong>, F-02, E-B &middot; <code>18:30</code> montage from A/B/C + B-18 &middot; <code>20:30</code> E-S &middot; <code>21:30</code> C-11a &middot; <code>22:20</code> E-B</td></tr>
    <tr><td><strong>EP 2</strong></td><td><code>00:00</code> C-13 (from F-01) &middot; <code>00:40</code> E-B &middot; <code>02:00</code> <strong>f4</strong>, D-05, <strong>G5</strong> &middot; <code>04:30</code> B-13, D-05 &middot; <code>07:00</code> E-B + whiteboard &middot; <code>09:00</code> E-B &middot; <code>10:00</code> C-06, <strong>f5</strong>, E-B &middot; <code>13:00</code> <strong>G3</strong> — <em>no capture</em> &middot; <code>16:00</code> E-B &middot; <code>19:00</code> trace plot — <em>no capture</em> &middot; <code>21:00</code> <strong>G6</strong> — <em>no capture</em> &middot; <code>22:30</code> <strong>g2a</strong>, <strong>g2b</strong>, F-01, E-B &middot; <code>25:30</code> D-03, D-04 &middot; <code>27:00</code> E-B &middot; <code>29:30</code> C-11b</td></tr>
    <tr><td><strong>EP 3</strong></td><td><code>00:00</code> A-05 &middot; <code>00:45</code> E-L &middot; <code>02:00</code> <code>keypad.svg</code>, E-L &middot; <code>04:00</code> rendered PNGs + one real panel shot &middot; <code>06:00</code> B-07, B-08, E-L &middot; <code>08:00</code> A-06 &middot; <code>10:00</code> B-09, E-L &middot; <code>12:00</code> E-S &middot; <code>12:30</code> A-07, A-08 &middot; <code>17:00</code> <strong>g2c</strong>, <strong>g7</strong>, E-L &middot; <code>20:30</code> A-09, A-10 &middot; <code>22:30</code> A-11, A-12, <strong>g2d</strong> &middot; <code>24:30</code> B-11 &middot; <code>26:30</code> A-13, B-10, F-04 &middot; <code>28:00</code> C-11c, E-L</td></tr>
    <tr><td><strong>EP 4</strong></td><td><code>00:00</code> C-01 &middot; <code>00:40</code> C-02, E-B &middot; <code>03:00</code> C-03, D-09 &middot; <code>06:30</code> D-01 &middot; <code>10:00</code> C-04, C-05 &middot; <code>13:00</code> C-07, C-08, B-13, E-B &middot; <code>17:00</code> B-12, E-B + <strong>g4b</strong> &middot; <code>19:00</code> C-14, E-B &middot; <code>21:00</code> B-14, B-15 + <strong>G4</strong> &middot; <code>24:30</code> C-10, B-16 &middot; <code>27:30</code> C-11d, E-B</td></tr>
    <tr><td><strong>EP 5</strong></td><td><code>00:00</code> B-18 &middot; <code>00:35</code> E-B &middot; <code>02:00</code> C-09 &middot; <code>05:00</code> D-02 &middot; <code>07:30</code> D-06, D-07 &middot; <code>10:00</code> D-06 + B-17, C-08 &middot; <code>15:30</code> B-17 &middot; <code>19:00</code> D-08, F-04 &middot; <code>21:30</code> table — <em>no capture</em> &middot; <code>23:00</code> E-L &middot; <code>24:30</code> C-11e</td></tr>
  </tbody>
</table>
```

This is the exact, complete table (all 5 rows) — use it directly as fixture data for tests;
don't re-derive it from a fresh read of the real file, which may have since changed slightly.

**Structural facts about this table, confirmed by inspection:**

- One `<tr>` per episode. Each cell is a `&middot;`-joined sequence of `<code>TIMECODE</code>
  <free text>` entries, one per beat, in the episode's own chronological order.
- **Only the timecode is `<code>`-tagged in the free text** (except `keypad.svg`, see below).
  Shot codes (`A-01`, `E-L`, `B-18`, etc.) are **plain text**. Animation/title codes are
  **`<strong>`-tagged** (`<strong>G1</strong>`, `<strong>g2a</strong>`).
- Shot-code notation seen: plain codes (`E-L`), numbered codes (`A-01`, `C-11a` — letter
  suffix), and a **shared-prefix slash list**: `A-01/02/03` means `A-01`, `A-02`, `A-03`.
- Free prose appears liberally and must be tolerated, not force-parsed: `"VO over the above"`,
  `"montage from A/B/C + B-18"` (one real code, `B-18`, embedded in prose — extract it, discard
  the rest), `"rendered PNGs + one real panel shot"` (no code at all), `"trace plot — no
  capture"` (no code), `"C-13 (from F-01)"` (the parenthetical is a derivation note, not a
  second covering code — only `C-13` is a code here).
- `<code>keypad.svg</code>` (EP3, `02:00`) is a firmware-repo docs file, not an
  `AssetType`/`Asset` this app knows about. It will not match any known asset code under the
  matching rules below, and that is correct — it should be silently skipped, not specially
  handled.
- `"— no capture"` (wrapped in `<em>`) appears next to `G3`, `G6`, and twice on bare prose
  (`"trace plot"`, `"table"`). **Do not treat this text as a semantic signal.** It describes
  production status, not linkability. Apply the same match-or-skip logic uniformly whether or
  not "no capture" is present — `G3` resolves to a real, already-built asset (`g3_cores`) and
  should link normally despite the annotation; `G6` and `G4` don't resolve to any asset (not
  yet built, per this repo's own open questions) and get skipped by the normal
  no-match-found path, with no special-casing needed either way.

## Data model change

**`AssetBeat` gains one new nullable column:**

```csharp
public class AssetBeat
{
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int BeatId { get; set; }
    public Beat? Beat { get; set; }
    public int? OrderInBeat { get; set; }
}
```

`OrderInBeat` is per-*occurrence* (the join row), not per-asset, because the same asset can
be linked to multiple beats at different narrative positions each time (confirmed: e.g. `E-L`,
`E-B` recur across many beats in the table above). `null` means "position unknown" — every
`AssetBeat` created by the pre-existing production-plan-based shot import starts `null` until
the new Coverage Check pass (below) fills it in where it can. Requires one new EF Core
migration (`dotnet tool install --global dotnet-ef --version 10.0.12` if the tool isn't
already installed globally — confirmed not installed in this dev environment as of this spec).

## Importer change: Coverage Check parsing

**New file: `src/ProductionBible.Importer/CoverageCheckParser.cs`**, following the existing
`StoryboardHtmlParser` pattern (static class, `HtmlAgilityPack` for the DOM). Parses the table
above into:

```csharp
public record CoverageCodeEntry(string Code, bool IsAnimation);
public record CoverageBeatEntry(int EpisodeNumber, string Timecode, List<CoverageCodeEntry> Codes);

public static class CoverageCheckParser
{
    public static List<CoverageBeatEntry> Parse(HtmlDocument doc) { ... }
}
```

`Codes` is **one combined list in true left-to-right document order**, each entry tagged
`IsAnimation` — not two separately-collected shot/animation lists merged afterward. This
matters: `"16:30 G5, F-02, E-B"` has the animation code (`G5`) appearing *first*, before two
shot codes. Collecting shots and animations into separate lists and concatenating them
afterward (shots-then-animations, or vice versa) would silently reorder this case — the
`OrderInBeat` values assigned later must come from each code's actual position in the
original text, so the parser has to preserve interleaving as it scans, not sort by type
after the fact.

(`HtmlDocument` — accept the already-parsed doc rather than re-parsing the HTML string, since
`StoryboardHtmlParser.Parse` already loads one; refactor `StoryboardHtmlParser.Parse` to load
the `HtmlDocument` once and pass it to both parsers, or expose the loaded doc — implementer's
call on the cleanest wiring, but do not parse the HTML string twice.)

**Locate the table:** find the `<h3>` whose text is exactly `"Coverage check"`, then its
following-sibling `.tablewrap` (same pattern `StoryboardHtmlParser.ParseShotRows` already uses
for `"Setup "` headings).

**Per `<tr>`:** extract the episode number from the first `<td>`'s text (`"EP 1"` → `1`, regex
`\d+`). Split the second `<td>`'s inner HTML on `&middot;` (after `HtmlEntity.DeEntitize`, this
becomes the literal `·` character — split on that). For each resulting segment:

1. Extract the leading `<code>TIMECODE</code>` — this is the beat's timecode for this entry.
2. From the **remaining HTML** (after removing that leading `<code>` element):
   - **Animation codes:** every `<strong>` element's inner text is one animation/title code
     candidate.
   - **Shot codes:** strip all tags to get plain text, then run the shared-prefix slash
     expansion first (regex `^(?<letter>[A-F])-(?<nums>\d+(?:/\d+)+)$` on each
     comma/plus-split token — if it matches, expand to `letter-num` for each num in `nums`),
     then keep only tokens matching `^[A-F]-[A-Za-z0-9]+$` (this single pattern covers
     `A-01`, `C-11a`, `B-18`, and the letter-suffixed piece-to-camera codes `E-L`/`E-B`/`E-S`).
     Split candidate tokens on `,` and `+` (both appear as separators in the real data — see
     `"B-05 + G1"`, `"D-06 + B-17, C-08"`). Discard anything that doesn't match — this is how
     `"VO over the above"`, `"montage from A/B/C"`, `"(from F-01)"`, `"whiteboard"`,
     `"rendered PNGs + one real panel shot"`, `"trace plot"`, `"table"`, and `keypad.svg` are
     correctly dropped without special-casing any of them by name.
3. Append each matched code — shot or animation — to the segment's single `Codes` list **in
   the order encountered while scanning the segment's text left-to-right**, tagging each with
   `IsAnimation`. E.g. `"B-12, E-B + g4b"` → `[("B-12", false), ("E-B", false), ("g4b", true)]`;
   `"G5, F-02, E-B"` → `[("G5", true), ("F-02", false), ("E-B", false)]` — the animation code
   stays first because it appears first in the text, regardless of type.

**Matching animation codes to assets** (needed because the table abbreviates:
`G1`→`g1_gears`, `g2a`→`g2a_problem`, `g4b`→`g4b_encoder`, while `f1`/`f3`/`f4`/`f5` are
already exact): match case-insensitively, trying (a) exact equality against a known
animation/title `Asset.Code` first, then (b) equality against the known code's substring
**before its first `_`** (`"g1_gears".Split('_')[0]` == `"g1"` == `"G1"` case-insensitively).
This single rule handles every real code in the table above without a hardcoded lookup table —
verify this claim against the actual current animation asset list at implementation time
(`g1_gears`, `g2a_problem`, `g2b_fix`, `g2c_workpiece`, `g2d_threadl`, `g3_cores`,
`g4b_encoder`, `g5_defaults`, `g7_runin`, `f1`, `f3`, `f4`, `f5`, plus `t1_title`..`t5_title`,
`l1_machinespec`, `l2_location` — none of the latter two groups appear in the Coverage Check
table's sample above, so no test coverage is expected for them from this data, but the
matching rule must not special-case away from covering them if they ever do appear). A
candidate with no match (`G4`, `G6`) is skipped with a `Console.WriteLine("Warning: ...")`,
consistent with the importer's existing warning style.

**Applying the parsed entries**, in `ImportMapper.ImportAsync`, after both existing loops
(shot pages, then animation rows) have run and all assets/AssetBeats from them exist:

- For each `CoverageBeatEntry`, resolve the `Beat` via the existing `_beatsByKey` dictionary
  keyed on `(episodeNumber, timecode)` — **do not create a new beat here** if none exists for
  that exact `(episode, timecode)` pair; log a warning and skip that entry instead (a
  timecode mismatch between the Coverage Check table and `production_plan.md`'s own meta
  lines would be a real data inconsistency worth surfacing, not silently papering over).
- Walk `Codes` in order, assigning sequential `OrderInBeat` values `0, 1, 2, ...` as you go —
  the list is already in the correct order per the parsing step above.
- For each entry where `IsAnimation` is `false`: find the existing `Asset` by `Code`
  (case-sensitive exact match, matching the existing shot-code convention) and the existing
  `AssetBeat` row linking that asset to this beat (it should already exist from the
  production-plan-based import — if it doesn't, log a warning and skip: this is the "table
  gives a timecode/code pairing the shot page's own timecodes didn't" case). Set
  `OrderInBeat` on that existing row.
- For each entry where `IsAnimation` is `true` and the code resolves to a known asset (see
  matching rule below): if an `AssetBeat` linking that asset to this beat doesn't already
  exist, create one (`_db.AssetBeats.Add(...)`) with `OrderInBeat` set. This is what fixes #1.

## Backend: beat ordering fix (#2)

**New file: `src/ProductionBible.Application/TimecodeOrdering.cs`:**

```csharp
public static class TimecodeOrdering
{
    // Matches "M:SS", "MM:SS", "H:MM:SS", "HH:MM:SS". Returns false (and 0) for anything else —
    // including the raw-prose "Unscheduled" sentinel case (EpisodeNumber = 0 pages whose
    // Timecodes value is free text, e.g. "Reused in EP1 20:30 · EP3 12:00 · ...").
    public static bool TryParseSeconds(string timecode, out int seconds)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            timecode, @"^(?:(?<h>\d+):)?(?<m>\d{1,2}):(?<s>\d{2})$");
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
}
```

**`BeatService.GetByEpisodeAsync`** changes from a pure SQL projection to fetch-then-sort in
memory (this app's data volumes are tens of beats per episode — this is not a performance
concern, and it avoids writing timecode-parsing logic in SQL):

```csharp
public async Task<IReadOnlyList<BeatDto>> GetByEpisodeAsync(int episodeId)
{
    var beats = await _db.Beats
        .Include(b => b.AssetBeats)
        .Where(b => b.EpisodeId == episodeId)
        .ToListAsync();

    return beats
        .Select(b => (Beat: b, Parsed: TimecodeOrdering.TryParseSeconds(b.Timecode, out var sec), Seconds: sec))
        .OrderBy(x => x.Parsed ? 0 : 1)   // parseable timecodes first, "Unscheduled" last
        .ThenBy(x => x.Seconds)
        .Select(x => new BeatDto(
            x.Beat.Id, x.Beat.EpisodeId, x.Beat.Timecode, x.Beat.Purpose,
            x.Beat.AssetBeats
                .OrderBy(ab => ab.OrderInBeat ?? int.MaxValue)
                .Select(ab => ab.AssetId)
                .ToArray()))
        .ToList();
}
```

Note this single change also delivers the ordering half of #11: `assetIds` within each
`BeatDto` is now sorted by `OrderInBeat` (nulls sort last, so any `AssetBeat` the Coverage
Check pass didn't touch — e.g. from a future manual edit via the API — still appears, just
after the ones with a known position).

`BeatService.GetByIdAsync` is unaffected (returns one beat; no list to order).

## Frontend: consume the now-ordered `assetIds` (#11)

**`web/src/app/bible/bible.component.ts`**, `assetsForBeat`, changes from a filter (which
preserves `this.assets`' own fetch order, not the beat's order) to a map over the
now-ordered `beat.assetIds`:

```typescript
assetsForBeat(beat: BeatDto): AssetDto[] {
  const byId = new Map(this.assets.map((asset) => [asset.id, asset]));
  return beat.assetIds
    .map((id) => byId.get(id))
    .filter((asset): asset is AssetDto => asset !== undefined);
}
```

The `.filter(...)` at the end is a defensive guard (an id present in `beat.assetIds` but
absent from `this.assets` shouldn't happen in practice, but silently dropping it is safer than
a runtime error over a data-consistency edge case that isn't this component's job to detect).

No other frontend file needs to change for #11 — `unassignedAssets`, the Production Plan view
(which correctly keeps using `SequenceNumber`), and the API client are all unaffected.

## Timeline redesign (#12)

Defer to [issue #12](https://github.com/martinlong1978/ProductionBible/issues/12) for the full
design — layout model, track columns, color tokens, non-goals, and task breakdown are all
specified there in detail and are not repeated here. One integration point this spec adds,
now that `OrderInBeat` exists (it didn't when that issue was drafted): within a beat's row,
order each track's clip(s) by the same `OrderInBeat` value the Bible view now uses, via
`beat.assetIds`' already-sorted order (from `BeatService`, above) — do not invent a separate
ordering scheme for the timeline.

## Non-goals

- No change to the Production Plan view's shooting-order display (`SequenceNumber` stays
  authoritative there).
- No change to the existing production-plan-based shot→beat linking mechanism — the Coverage
  Check pass is additive (fills gaps, adds ordering), never replaces it, per the confirmed
  design decision.
- No UI for manually editing `OrderInBeat` — it's importer-populated only, for now.
- No attempt to interpret "no capture" text as applicaton-visible status; it's prose, not data,
  for this pass.
- No change to `AssetService` or `AssetDto` — ordering is carried entirely by `BeatDto.assetIds`'
  order, not a new field on the asset itself.

## Testing

Standard rigor for this piece of work (no "minimal testing" direction was given, unlike the UI
redesign) — real fixture-based tests for the new parser using the exact table HTML embedded
above, unit tests for `TimecodeOrdering.TryParseSeconds` (padded, unpadded, `HH:MM:SS`, and the
Unscheduled raw-prose case), `BeatService` tests confirming ordering and `OrderInBeat`
propagation, and updated `BibleComponent` tests confirming `assetsForBeat` now returns assets
in `beat.assetIds`' order rather than `this.assets`' fetch order.
