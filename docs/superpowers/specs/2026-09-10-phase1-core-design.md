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
Hierarchy: **Project → Episode → Asset**, revised from an earlier Shot-only model per
Martin's correction below. This is the most significant schema decision in Phase 1.

- **REVISION (Martin, 2026-09-10):** Shot and VO/Beat should not be modelled as two
  separate kinds of thing at the top level. VO is just as much "an asset that needs to
  be created" as a camera shot is — the difference is only *how* it's created (camera vs
  microphone vs a rendered animation vs a title card). So the atomic production unit is
  a generalized **Asset**, with a type: `Shot`, `VoiceOver`, `Animation`, `Title`,
  `Graphic`, `Flyover`, etc. This matches how `storyboard.html` already organizes things
  by lettered series (A/B/C/D = camera shot series, E = pieces to camera / VO, F =
  credit flyovers, G = graphics/animations, T = title cards) — they're all "things that
  need to be produced," just via different means.
- **Beat** is the timecode/narrative anchor — episode + timecode + narrative purpose
  (what today is the outline-script beat sheet, e.g. "EP1 04:00"). Confirmed
  **many-to-many** with Asset: a Beat can reference several Assets (A-01/02/03, B-01
  all under one beat), and an Asset can in principle serve more than one Beat (e.g.
  reused b-roll cut into two different moments).
- **CORRECTION (Martin, 2026-09-10):** the "machine configuration" column from
  `storyboard.html` is HalfNut-ELS-specific vocabulary (lathe/thread/rpm settings).
  The generalized, domain-agnostic version of this field is **"Scene/subject setup"**
  — whatever needs to be arranged/staged before the asset is created (a machine's
  configuration, a prop's position, a screen's state, wardrobe, a render parameter set
  — whatever applies to the production/asset type). Do not hard-code machining terms
  into the schema, labels, or seed-import mapping logic beyond the imported *data*
  itself.
- Multi-project / multi-episode support is native to the schema from the start (not
  retrofitted later) — this was requested explicitly and Project is the top-level
  entity.
- **Open technical question (schema shape for type-specific fields):** Shot-type assets
  need location/angle-camera/audio fields that don't apply to a Title asset; Animation
  assets need a source-script reference that doesn't apply to a Shot. Need to decide
  between (a) one Asset table, common fields only, type-specific extras in a flexible
  key/value AssetAttribute table, (b) one Asset table (EF Core TPH) with nullable
  type-specific columns, (c) Asset base table + one linked table per type (EF Core TPT).
  **Not yet asked — next question.**

### Tech stack
- Backend: ASP.NET Core (current LTS at build time — check .NET version when
  implementation starts), EF Core, SQLite.
- Frontend: Angular SPA, built and served as static files from the same Kestrel process.
- Backend and frontend still developed as normal separate projects in the repo (Angular
  CLI app + .NET web project) — "one process" refers to *runtime*, not to merging the
  source trees.

---

### View modes (confirmed)
- **Bible** and **Production Plan** are two independent screens/orderings over the same
  Asset data (episode/timecode order vs shoot-day order grouped by phase/setup).
- **Timeline is a display mode of the Bible view**, not a third independent screen — same
  episode/timecode ordering, rendered as vertical stacked tracks (A-roll/B-roll/
  titles/animations) instead of prose, toggled within Bible.

### Visual identity (confirmed)
- The app gets its **own distinct visual identity**, not the HalfNut ELS storyboard
  palette — it needs to work for future non-HalfNut-ELS projects too. Design approach
  TBD when UI work starts (separate concern from this data/API-focused spec).

### Asset field shape (confirmed)
- **Common table + flexible key/value attributes.** Asset carries core fields shared by
  every type (code, type, title, script text, status, notes, sequence number, target
  length). Type-specific fields (Shot's location/angle-camera/audio, Animation's source
  script reference, etc.) live in an `AssetAttribute` table (`asset_id`, `key`, `value`).
  New asset types or one-off fields need zero schema migration — matches the
  MCP-driven, evolving-schema goal explicitly.

### Authoring style (confirmed)
- **Structured fields only for Phase 1.** Forms in the UI, typed fields via API/MCP. A
  free-text notes field can still exist per asset, but no document-style bulk/markdown
  editor in Phase 1.

### Printable export (confirmed)
- **Deferred to a later phase.** Phase 1 is data model + API + on-screen UI only. Print/
  PDF export (replacing `storyboard.pdf` / `production_plan.pdf`) becomes its own small
  phase once the data model has settled from real use.

### MCP tool surface (decided, not asked — matches explicit original request)
- **Full CRUD parity with the REST API.** The brief said "full control should be
  possible by MCP" — no reason to curate a subset. MCP tools call the same service
  layer as the REST controllers (per the earlier in-process hosting decision), so parity
  is close to free.

## Open questions (remaining, judgment calls to present in the full design, not separately asked)

- .NET / Angular version pins — propose "latest stable LTS at implementation time"
  rather than asserting a specific version number now (matches this project's own
  convention of not asserting unverified facts).
- Exact REST endpoint shapes, exact Asset/Beat/AssetAttribute field lists, exact import
  script mapping rules from `storyboard.html` / `production_plan.md`.
- Error handling / validation expectations.
- Testing approach for Phase 1.

**Next step:** synthesize all of the above into a full sectioned design, present in chat
for approval, then write the final spec (this file becomes that spec once approved).

---

## Next steps

Continue clarifying-question pass (Beat/VO relationship, view-mode specifics, MCP
surface, UI library/palette), then propose 2–3 architecture approaches for anything
still undecided, then present the full sectioned design here for approval, then this doc
becomes the final spec.
