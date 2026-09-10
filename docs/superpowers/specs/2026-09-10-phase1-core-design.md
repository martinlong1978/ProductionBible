# Phase 1 Design — Core data model, API, and multi-mode UI

**Status:** Approved by Martin, 2026-09-10. Ready for implementation planning.

## Context

**ProductionBible** is a local web application for planning and running video
production — unifying a "bible" (storyboard/beat sheets/scripts), a shoot-day
production plan, and script sheets into one dataset, so the two documents stop
drifting out of sync (today, `storyboard.html` is missing shot IDs that
`production_plan.md` has, because they're maintained as two separate hand-edited
files). It is designed to generalize beyond one production: multi-project,
multi-episode from the start.

The HalfNut ELS YouTube series (`D:\Data\source\HalfNutELS-Video`) is the source of
real test data and the design template — its `storyboard.html` (57+ shot list, beat
sheets, outline scripts, ready-to-cut gates) and `production_plan.md` (65
sequences, one page per shot, grouped by shoot phase/setup) are what Phase 1's
import script ingests.

**Phasing** (each phase gets its own design → spec → plan → implementation cycle;
this document covers Phase 1 only):

1. **Phase 1 (this doc):** core data model, CRUD API, Angular UI (Bible view with a
   Timeline display mode, and Production Plan view), one-time import of the existing
   HalfNut ELS content as seed data.
2. Phase 2: MCP server, in-process, wrapping the same service layer as the REST API.
3. Phase 3: shoot-day tooling — Shot Complete button + timestamp capture, session/take
   tracking.
4. Phase 4: media file matching (video/audio indexing, optional renaming, DaVinci
   Resolve investigation — likely its own spike first, since Resolve Free has no
   scripting API).
5. Phase 5: teleprompter push integration — HTTP calls to the existing Flask
   `upload_server.py` on `teleprompt.lan:8080` (`POST /write`, `POST /launch`).

## Explicitly out of scope for Phase 1

MCP server, Shot Complete button/timestamps, media file matching, Resolve
integration, teleprompter push, printable export (PDF/HTML field documents),
markdown/rich-text authoring, authentication.

## Architecture

One .NET process serves everything:

- **Backend:** ASP.NET Core Web API, EF Core, SQLite. Latest stable .NET LTS at
  implementation time (do not pin a version number in this spec — verify what's
  current when the implementation plan is written).
- **Frontend:** Angular SPA (latest stable Angular at implementation time), built and
  served as static files from the same Kestrel process. Angular and the API are still
  separate projects/source trees in the repo; "one process" describes runtime, not
  source layout. `ng serve` proxying to the API remains the normal dev-time workflow.
- **Network:** binds to `0.0.0.0`, reachable from other devices on the LAN (phone/
  tablet at the bench, the teleprompter Pi). No authentication — trusted home network,
  same trust model as the teleprompter's own Flask server.
- **Service layer:** business logic lives in a service layer beneath the API
  controllers, not inline in controller actions — because Phase 2's MCP tools will
  call the same service layer directly rather than round-tripping through HTTP to
  itself. Get this boundary right in Phase 1 even though MCP doesn't exist yet.
- **Visual identity:** its own distinct look, not the HalfNut ELS storyboard palette —
  the app needs to work for future non-HalfNut-ELS projects too. Concrete UI/component
  choices are left to implementation (structured fields only, no document editor, is
  the only UI constraint fixed here).

## Data model

```
Project
  └─ Episode                    (ordered within Project)
       ├─ Beat                  (timecode anchor: episode + timecode + narrative purpose)
       └─ Asset                 (the atomic production unit)
              ├─ AssetType      (reference table: Shot | VoiceOver | Animation |
              │                  Title | Graphic | Flyover | ... — extensible, not
              │                  a hard-coded enum)
              ├─ core fields:
              │     Code                 e.g. "A-01", "F-01", "G2a", "T1"
              │     Title
              │     ScriptText           nullable — not every asset has dialogue/VO
              │     Status               e.g. Planned / Scripted / Shot / Rendered /
              │                          Complete (exact value set: implementation detail)
              │     Notes                free text, filled in after the take/render
              │     SequenceNumber       shoot/production order (nullable — the
              │                          equivalent of production_plan.md's 1–65)
              │     TargetLengthSeconds  nullable
              │     CompletedAtUtc       nullable; column exists in Phase 1 so Phase 3's
              │                          Shot Complete button needs no migration
              └─ AssetAttribute[]  (AssetId, Key, Value — type-specific fields live
                  here, not as columns, so new asset types or one-off fields need no
                  schema migration)

AssetBeat   (join table: many-to-many, Asset ↔ Beat — a Beat can reference several
             Assets, e.g. A-01/02/03 + B-01 all under one beat; an Asset can in
             principle serve more than one Beat, e.g. reused b-roll)
```

Rationale for the flexible-attribute shape over strongly-typed columns (TPH/TPT):
the brief calls for AI agents to have full control via MCP, including — plausibly —
inventing new asset types or one-off fields later. A key/value `AssetAttribute`
table means that happens without a code change or migration. The trade-off (weaker
query/validation typing at the DB level) is acceptable at this data volume (low
hundreds of rows per project) and is mitigated by validation living in the service
layer, not the database schema.

**Known AssetAttribute keys for Phase 1's Shot type**, derived directly from
`production_plan.md`'s per-shot fields (generalized off machining vocabulary per the
correction below): `Location`, `SceneSetup`, `AngleAndCamera`, `AudioNotes`,
`AdditionalConsiderations`, `PhaseGroup`. Animation-type assets get a
`SourceScriptRef` attribute (e.g. pointing at an `anim/scenes.py` function name).
This list is a starting point for the import script, not a closed set — the schema
does not enforce which keys exist for which type.

**Domain-neutrality constraint:** `storyboard.html`'s "machine configuration" column
is HalfNut-ELS-specific (lathe/thread/rpm settings). Its generalized equivalent here
is `SceneSetup` — whatever needs to be arranged or staged before an asset is created
(a machine's configuration, a prop's position, a screen's state, wardrobe, a render
parameter set). Schema, field labels, and import-mapping *code* must stay
domain-neutral; only the imported *data values* are HalfNut-ELS-specific.

## API

Standard REST resource endpoints, full CRUD on each:

- `/api/projects`
- `/api/projects/{id}/episodes`
- `/api/episodes/{id}/beats`
- `/api/episodes/{id}/assets`
- `/api/assets/{id}` — includes nested attributes and linked beats
- `/api/asset-types` — the extensible type registry

This is deliberately also the exact surface Phase 2's MCP tools will wrap 1:1,
because they call the same service layer.

## Import script

A one-time console tool (or a dev-only API endpoint) that parses:

- `storyboard.html` — shot tables (code, episode/timecode, capture notes, "machine
  configuration" → mapped to `SceneSetup`), beat/outline-script sections, open
  questions
- `production_plan.md` — per-shot pages (Location, Setup, Angle & camera, Audio,
  Target length, Script, Additional considerations, sequence number, phase grouping)

and cross-links the two by shot code (e.g. `A-01`) into one Project → Episode →
Beat/Asset/AssetAttribute graph, producing the ~65 real HalfNut ELS assets as seed
data. This is a **one-time** import against a fresh database — not a live sync. Once
imported, the database is the source of truth; the original files remain as
historical/reference documents only.

## UI

Two screens over the same underlying data:

- **Bible view** — episode/timecode order (matches `storyboard.html`'s structure
  today). Includes a **Timeline display mode** toggle that re-renders the same
  ordered data as vertical stacked tracks (A-roll / B-roll / titles / animations) in
  a DaVinci-Resolve-timeline-like layout, rather than as prose/tables. This is a view
  toggle within Bible, not a separate screen.
- **Production Plan view** — shoot-day order, grouped by phase/setup (matches
  `production_plan.md`'s structure today).

Editing is structured-field-only in Phase 1 (forms in the UI, typed fields via
API/MCP) — no markdown/rich-text document editor. A free-text `Notes` field exists
per asset for anything that doesn't fit structured fields.

## Testing approach

- **Unit tests** on the service layer and on the import script's parsing logic — the
  real HalfNut ELS files are a natural fixture for the latter.
- **Integration tests** on the API against an in-memory or temp-file SQLite database.
- **Angular component tests** for the Bible and Production Plan views, including the
  Timeline display-mode toggle.
- No end-to-end/browser test framework in Phase 1 — YAGNI until the UI stabilizes;
  revisit if Phase 3 (shoot-day, time-sensitive UI) needs it.

## Decisions log (for reference — all confirmed with Martin during brainstorming)

| Decision | Choice |
|---|---|
| Repo location | New sibling repo `D:\Data\source\ProductionBible` |
| Network scope | LAN-reachable (`0.0.0.0`), no auth |
| Data migration | One-time import script, not a live sync |
| Core hierarchy | Project → Episode → Asset (revised from an initial Shot-only model) |
| Beat↔Asset relationship | Many-to-many join table |
| Asset field shape | Common table + flexible `AssetAttribute` key/value, not TPH/TPT |
| "Machine configuration" field | Generalized to `SceneSetup`, domain-neutral |
| Process model | One process: ASP.NET Core serves API + static Angular build |
| MCP hosting (Phase 2) | In-process, same app, same service layer |
| Timeline view | Display mode of Bible view, not a separate screen |
| Visual identity | Own distinct identity, not the HalfNut ELS palette |
| Authoring style | Structured fields only, no document editor, in Phase 1 |
| Printable export | Deferred to a later phase |
| MCP tool surface (Phase 2) | Full CRUD parity with the REST API |
