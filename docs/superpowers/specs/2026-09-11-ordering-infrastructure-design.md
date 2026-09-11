# Ordering Infrastructure Design

## Context

This is sub-project #2 of a larger UI-expansion effort (rename Bible→Storyboard,
full CRUD across all entities, Storyboard/Production-Plan reorder workflows, a
new Asset table view). It was split out first because sub-projects #3-5 all
need it: reorderable Beats, a real `Phase` entity, and reorderable shots within
a beat or phase all require schema and API work that doesn't exist today.

**Scope boundary — read this first:** this spec is **backend and data-model
only**. No Angular changes. The new reorder endpoints below are built and
tested, but nothing in the UI calls them yet — that's sub-projects #3
(generic CRUD UI), #4 (Storyboard workflow: beat reorder, shot-within-beat
reorder), and #5 (Production Plan workflow: Phase CRUD+reorder,
shot-within-phase reorder).

Two real bugs surfaced while reviewing the just-merged import-ordering-timeline
work, and both motivate this redesign rather than being fixed as one-off patches:

- **EP1's A-01/A-02 order.** The Coverage Check table's `A-01/02/03` shorthand
  expands in literal written-digit order, but the shot list itself says A-02 —
  described as "the first frame of the series" (spindle spin-up, the cold
  open) — is the true first shot, with A-01 a mid-pass cutting shot. No text
  in the Coverage Check row encodes true order; regex cannot resolve this.
- **`t1_title` never linked to any beat.** It appears only in the Animation
  clip catalog table and in prose inside a beat's own paragraph — never in
  the Coverage Check table, which is the importer's only source of
  animation-beat links today. Not a parser bug: the Coverage Check table is
  incomplete by construction for title cards.

Both point at the same conclusion: regex-only parsing of `storyboard.html`/
`production_plan.md` has a real ceiling, and getting genuinely correct order
means either manual correction after import, or a smarter one-time
LLM-assisted extraction pass. That extraction work (see "Deferred: LLM-assisted
seed extraction" below) is out of scope for this spec, but this spec's schema
changes (`Ordinal`, `DurationSeconds`, real `Phase` rows) are exactly the
fields a future manual-correction or seed.json workflow would edit — so this
is also the prerequisite for that idea, not just for the UI reorder work.

## Data model changes

### `Beat`

```csharp
public class Beat
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    public string SourceTimecode { get; set; } = "";   // renamed from Timecode
    public int Ordinal { get; set; }                    // NEW — defines order within the episode
    public int DurationSeconds { get; set; }             // NEW
    public string Purpose { get; set; } = "";

    public List<AssetBeat> AssetBeats { get; set; } = new();
}
```

- `SourceTimecode` is a read-only reference back to whatever timecode string
  storyboard.html actually printed (e.g. `"05:40"`, or the literal prose for
  the one `EpisodeNumber = 0` "Unscheduled" sentinel row). It is still the
  import-time dedup key (`_beatsByKey` keeps its `(episodeNumber, timecode)`
  shape, just reading from `SourceTimecode`). It is never parsed for ordering
  or timing again after import. **The rename is C#-entity-internal only** —
  `BeatDto`'s outward field stays named `Timecode` (so the JSON shape, and
  every existing Angular consumer of `beat.timecode`, is unaffected). This
  keeps the "no Angular changes" boundary literally true: the new fields
  below are purely additive to `BeatDto`, which TypeScript's structural
  typing ignores until sub-project #4 opts a component into reading them.
- `Ordinal` is the sole ordering key within an episode. Lower = earlier.
  Manually reorderable via the new reorder endpoint (§ API surface).
- `DurationSeconds` is a plain persisted value, freely editable. It is *not*
  recomputed from anything at read time — once set (at import, or later by a
  human), it stays until explicitly changed.
- **Start/end time is never stored.** `BeatService` computes
  `StartSeconds`/`EndSeconds` for a beat on read as the cumulative sum of
  `DurationSeconds` for every beat in the same episode with a lower
  `Ordinal`, plus that beat's own `DurationSeconds` for `EndSeconds`. Add
  both to `BeatDto` as computed fields (not stored columns).

### `Phase` (new entity)

```csharp
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

- Scoped to `Project` (matches `Episode`'s existing scoping — Phases span
  episodes, per `production_plan.md`'s own "ordered by setup, not episode"
  organization).
- `Asset` gains `public int? PhaseId { get; set; }` + `public Phase? Phase { get; set; }`,
  replacing `Attributes["PhaseGroup"]`. Nullable because not every Asset
  (e.g. Animation, Title assets) belongs to a shoot-day phase.
- New `PhasesController` + `PhaseService`, same CRUD shape as
  `EpisodesController`/`EpisodeService`: `GetByProjectAsync`, `GetByIdAsync`,
  `CreateAsync`, `UpdateAsync`, `DeleteAsync`.

### Shot ordering — no new fields needed

- **Shot-within-phase**: reuse `Asset.SequenceNumber` exactly as it works
  today (`phase-grouping.ts` already sorts by it). Only new work is a reorder
  endpoint that reassigns it across one phase's assets.
- **Shot-within-beat**: `AssetBeat.OrderInBeat` (added in the prior plan)
  already does this job. Only new work is a reorder endpoint — today only the
  importer sets it; nothing lets the UI set it.

## API surface

New reorder endpoints, all taking an ordered array of IDs and reassigning the
relevant ordinal field to array position (0-based):

| Endpoint | Reassigns |
|---|---|
| `PATCH /episodes/{episodeId}/beats/reorder` | `Beat.Ordinal` for beats in that episode |
| `PATCH /projects/{projectId}/phases/reorder` | `Phase.OrderIndex` for phases in that project |
| `PATCH /phases/{phaseId}/assets/reorder` | `Asset.SequenceNumber` for assets in that phase |
| `PATCH /beats/{beatId}/asset-beats/reorder` | `AssetBeat.OrderInBeat` for that beat's links |

Each takes `{ orderedIds: int[] }`, validates every ID belongs to the named
parent (400 if not, matching this app's existing validation style), and
returns the updated list in its new order. Each is a single `SaveChangesAsync`
covering all reassignments — no partial-reorder state should ever be visible.

New/changed CRUD DTOs:

- `BeatDto` gains `Ordinal`, `DurationSeconds`, `StartSeconds` (computed),
  `EndSeconds` (computed) as new fields. Its existing `Timecode` field is
  unchanged in name and value (still reads from the entity's
  `SourceTimecode` property) — purely additive, no breaking change.
- New `PhaseDto` (`Id`, `ProjectId`, `Name`, `OrderIndex`).
- `AssetDto`/`CreateAssetRequest`/`UpdateAssetRequest` gain `PhaseId` (nullable
  int), alongside the existing fields — `Attributes["PhaseGroup"]` is no
  longer written by the importer and can be ignored/removed from any UI that
  reads it (production-plan.component.ts's `phase-grouping.ts` becomes
  sub-project #5's job to rewire onto `PhaseId`, not this spec's).

## Importer changes

**Beats**: keep `TimecodeOrdering.TryParseSeconds` exactly as-is for reading
`SourceTimecode`. After all of an episode's beats are created (shot pages'
own timecodes + Coverage Check's pure-graphic beats), sort them by parsed
seconds — unparseable ones (the "Unscheduled" sentinel) sort last, matching
`BeatService.GetByEpisodeAsync`'s existing `OrderBy(x => x.Parsed ? 0 : 1)`
convention. Assign `Ordinal` = position in that sorted order (0-based).
Assign `DurationSeconds` = gap in parsed-seconds to the next beat in that
order; 60-second fallback for the last beat in the episode. This is the same
formula `web/src/app/bible/timeline-model.ts`'s `buildTimeline()` already
computes client-side — ported to C#, computed once at import, and persisted
instead of recomputed on every Timeline render. (`timeline-model.ts` itself
is not required to change in this spec — it can keep deriving row height from
whatever `DurationSeconds` the API now returns directly, once sub-project #4
wires the Timeline view onto the new field. Until then it keeps working
exactly as it does today, deriving from `SourceTimecode`.)

**Phases**: `GetOrCreatePhase(project, name)`, same pattern as
`GetOrCreateEpisode` — a `Dictionary<(int projectId, string name), Phase>`
keyed on first sight. `OrderIndex` = insertion order (matches today's de facto
ordering: `phase-grouping.ts`'s `Map` preserves first-seen order after
sorting all assets by `SequenceNumber`). Set `asset.PhaseId` directly from the
created/looked-up `Phase`; stop calling
`AddAttribute(asset, "PhaseGroup", page.PhaseGroup)`.

## Migration & rollout

One EF Core migration covering: `Beat.Timecode` → `Beat.SourceTimecode`
rename, `Beat.Ordinal`/`Beat.DurationSeconds` additions, new `Phases` table,
`Asset.PhaseId` FK addition. Per your answer, no data-preservation path is
needed — delete `src/ProductionBible.Api/App_Data/productionbible.db*`, run
the app once to apply the migration, then reseed via the importer as usual.
Old `AssetAttribute` rows with `Key = "PhaseGroup"` simply never get
recreated (the importer stops writing them) — nothing to explicitly clean up
since the DB is being wiped anyway.

**Known interim consequence, expected and acceptable:** once reseeded,
`Attributes["PhaseGroup"]` is absent from every asset. `phase-grouping.ts`
already falls back to `'Unphased'` when that key is missing (existing
behavior, not new code), so the Production Plan view will show everything
under one "Unphased" heading until sub-project #5 rewires it onto `PhaseId`.
This is a real, visible regression in that view for the gap between this
spec landing and #5 landing — acceptable since this is a solo-developer
project and the two land close together, but worth merging #5 promptly
rather than leaving it stale.

## Testing

- `TimecodeOrdering`/import-time Ordinal+Duration assignment: unit tests over
  a small multi-beat episode fixture, asserting the computed gaps and the
  60-second last-beat fallback, plus the Unscheduled-sorts-last case.
- `PhaseService`: standard CRUD unit tests, matching `EpisodeServiceTests.cs`'s
  existing shape.
- Each reorder endpoint: an integration test (real SQLite, per the precedent
  set by `ImportMapperSqliteTests.cs` — reorder endpoints mutate multiple rows
  in one transaction, exactly the kind of thing that hid a bug behind
  EF InMemory's forgiving ID semantics once already on this project) covering
  a full reorder, and a validation test for an ID that doesn't belong to the
  named parent.
- `BeatService.GetByEpisodeAsync`'s computed `StartSeconds`/`EndSeconds`:
  unit test against a small fixture with known `Ordinal`/`DurationSeconds`
  values, asserting the cumulative-sum arithmetic directly.

## Deferred: LLM-assisted seed extraction

Not part of this spec's implementation, recorded here so it isn't lost.
Rather than continuing to harden regex rules against unstructured prose (the
A-01/A-02 and `t1_title` bugs above are both this class of problem), a better
path once this schema lands: dispatch two subagents in parallel — one reading
`storyboard.html` directly with an LLM's judgment (not regex) to extract every
asset/beat/order relationship it can find, including the ones like A-02's true
position that no regex rule can recover; another reading the prior session
transcripts that originally *wrote* storyboard.html's content, which may carry
structural intent (why A-02 comes first) that the document text alone doesn't
state. Reconcile both against the regex-parsed import into a single
`seed.json`, with pointers back to the source document (e.g. a heading anchor
or line reference) so a human correction is a one-line json edit rather than a
regex-rule change. This becomes the actual seed source once built, parked as
a follow-up, not blocking sub-projects #3-5.

## Out of scope (explicitly)

- Any Angular/UI work — sub-projects #3-5.
- Rewiring `phase-grouping.ts` onto `PhaseId` — sub-project #5.
- Rewiring `timeline-model.ts` onto `DurationSeconds` — sub-project #4.
- The LLM-assisted seed extraction above.
- Fixing the two specific bugs (A-01/A-02 order, `t1_title` unlinked) as data
  — this spec only builds the fields a future correction would use; actually
  setting A-02's correct `Ordinal` or linking `t1_title` to its beat happens
  either via the seed-extraction follow-up or a manual edit once sub-project
  #4's reorder UI exists.
