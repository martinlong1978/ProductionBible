# Phase 1 Design — Core data model, API, and multi-mode UI

**Status:** DRAFT — brainstorming in progress. This file is being updated incrementally
as decisions are confirmed with Martin, specifically so a fresh session can resume from
git state if the brainstorming conversation is compacted or interrupted.

**Project:** ProductionBible — a local web app for planning and running video production
(storyboard/bible, shoot-day production plan, scripts), built using the HalfNut ELS
YouTube series (`D:\Data\source\HalfNutELS-Video`) as source test data and design template.
Domain-agnostic: nothing in the data model should assume machining/lathe specifics, since
the app is meant to generalize to other productions.

**Phasing (agreed):**
1. **Phase 1 (this doc):** core data model + CRUD API + Angular UI (bible/plan/timeline
   view modes) + printable export. One-time import of `storyboard.html` +
   `production_plan.md` as seed data.
2. Phase 2: MCP server, in-process, same app, same service layer as REST API.
3. Phase 3: shoot-day tooling — Shot Complete button + timestamp capture, session/take
   tracking.
4. Phase 4: media file matching (video/audio indexing, optional renaming, DaVinci Resolve
   investigation — likely needs its own spike).
5. Phase 5: teleprompter push integration (small — HTTP calls to the existing Flask
   `upload_server.py` on `teleprompt.lan:8080`, `POST /write` + `POST /launch`).

Each phase gets its own design → spec → plan → implementation cycle. This document covers
Phase 1 only.

---

## Decisions confirmed so far

### Repo & deployment
- New sibling repo: `D:\Data\source\ProductionBible` (this repo). Not nested inside the
  HalfNutELS-Video folder — this is a reusable tool, not video-specific content.
- **LAN-reachable, no auth.** Binds to `0.0.0.0` from the workstation so a phone/tablet at
  the bench (or the teleprompter Pi) can reach it during a shoot. Trusted home network,
  same trust model as the teleprompter's own Flask server. No login.
- **One process.** ASP.NET Core Web API + EF Core + SQLite backend. Angular built and
  served as static files from the same Kestrel process — one thing to run, one port to
  expose on the LAN.
- **MCP in-process, same app** (Phase 2, not built yet, but the hosting decision affects
  Phase 1's project layout). Use the official ModelContextProtocol C# SDK, exposed
  alongside the REST API from the same ASP.NET Core process, calling the same service
  layer as the REST controllers — not a separate process.

### Data migration
- **One-time import script** parses `storyboard.html` and `production_plan.md` once, at
  setup time, to populate the DB with the ~65 real HalfNut ELS shots as seed/test data.
  Not a live sync — after import, the DB is the source of truth and the old files are
  historical reference only.

### Core data model
Hierarchy: **Project → Episode → Shot**, confirmed as matching Martin's mental model,
with one correction (see below).

- **Shot** is the atomic unit. Carries the union of what today lives split across
  `storyboard.html`'s shot tables and `production_plan.md`'s per-shot pages:
  - Identity: shot code (e.g. `A-01`), sequence number within shoot order (1–65 today)
  - Storyboard-side fields: episode + timecode reference(s), narrative/beat context,
    capture note
  - Shoot-plan-side fields: location, setup/phase group, angle & camera, audio to
    capture, target length, script (if any), additional considerations, production
    notes (free text, filled in after the take), completion timestamp (Phase 3)
  - **CORRECTION (Martin, 2026-09-10):** the "machine configuration" column from
    `storyboard.html` is HalfNut-ELS-specific vocabulary (lathe/thread/rpm settings).
    The generalized, domain-agnostic version of this field is **"Scene/subject setup"**
    — whatever needs to be arranged/staged in front of the camera before the shot rolls
    (a machine's configuration, a prop's position, a screen's state, wardrobe — whatever
    applies to the production). Do not hard-code machining terms into the schema, labels,
    or seed-import mapping logic beyond the imported *data* itself.
- **Beat/VO** is a separate entity for narration that spans multiple shots or has no
  shot at all (cold opens, framing VO, outline-script beats keyed by timecode). Not
  merged into Shot. Relationship to Shot(s) still to be pinned down precisely (likely
  many-to-many or a loose timecode-based association) — **open, needs a follow-up
  question.**
- Multi-project / multi-episode support is native to the schema from the start (not
  retrofitted later) — this was requested explicitly and Project is the top-level
  entity.

### Tech stack
- Backend: ASP.NET Core (current LTS at build time — check .NET version when
  implementation starts), EF Core, SQLite.
- Frontend: Angular SPA, built and served as static files from the same Kestrel process.
- Backend and frontend still developed as normal separate projects in the repo (Angular
  CLI app + .NET web project) — "one process" refers to *runtime*, not to merging the
  source trees.

---

## Open questions (not yet asked / not yet resolved)

- Exact shape of the Beat/VO ↔ Shot relationship.
- View-mode specifics: what exactly differs between "bible" (timeline/episode order),
  "production plan" (shoot order, grouped by phase/setup), and "timeline" (vertical
  DaVinci-style stacked A-roll/B-roll/titles/animations) — is timeline mode a third
  independent view, or a visual variant of one of the other two?
- Printable export format: keep the current HTML→PDF pipeline's look, or a fresh design?
- MCP tool surface: full CRUD parity with the REST API, or a curated subset of tools?
- Angular version / UI component library (Material? something else?) and whether to
  carry over the storyboard's existing palette (`#84A895`, `#66B7CE`, `#E0A344`,
  `#B695BA`, `#0D1311`).
- Authoring workflow: does the app also need free-text/markdown editing capability
  comparable to how storyboard.html / production_plan.md are edited today, or is
  structured-field editing sufficient?
- .NET version pin, Angular version pin, EF Core migrations strategy.
- Error handling / validation expectations (e.g. required fields per shot).
- Testing approach and coverage expectations for Phase 1.

---

## Next steps

Continue clarifying-question pass (Beat/VO relationship, view-mode specifics, MCP
surface, UI library/palette), then propose 2–3 architecture approaches for anything
still undecided, then present the full sectioned design here for approval, then this doc
becomes the final spec.
