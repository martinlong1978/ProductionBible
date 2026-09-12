# MCP Server Design

## Context

This is Track A Phase 2 (see `CLAUDE.md`, "Remaining roadmap" → Track A). The
original Phase 1 design doc
(`docs/superpowers/specs/2026-09-10-phase1-core-design.md`) named this phase
and deliberately kept the service layer (`ProductionBible.Application`)
independent of the REST API controllers specifically so an MCP server could
sit alongside the REST API later without any service-layer rework. That
groundwork is unchanged and is what this phase now uses.

**Goal, confirmed directly:** full CRUD parity with the REST API, covering
all three of Martin's stated use cases — conversational data
entry/editing (e.g. "mark C-14 as Shot"), bulk/scripted operations, and
read-only querying/reporting. No use case is out of scope; nothing here is
read-only-only.

**Hosting:** in-process with the existing API, not a separate
deployable — unlike `ProductionBible.Importer` or the planned
`ProductionBible.MediaAgent`, this does not get its own project. It's the
same `ProductionBible.Api` process, same Kestrel instance, same no-auth
LAN-trust model the REST API already uses (see `CLAUDE.md`'s "Architecture"
section on Phase 1's `0.0.0.0` binding and trust model). Reachable at
`http://<host>:5280/mcp` alongside the existing `/api/...` routes.

**Package:** the official C# MCP SDK,
[`ModelContextProtocol.AspNetCore`](https://github.com/modelcontextprotocol/csharp-sdk)
(NuGet), added to `src/ProductionBible.Api/ProductionBible.Api.csproj`. Do
not pin an exact version in this spec or the implementation plan — verify
the current stable version when the plan is written, matching this
project's existing convention for framework versions (see Phase 1's own
spec on this point).

**Transport:** Streamable HTTP (the SDK's modern default,
`HttpServerSessionMode.Stateless`), not classic SSE. Stateless mode is
recommended by the SDK for servers that don't need server-initiated
requests (sampling, elicitation) — this server never needs those, since
every tool call is a synchronous request/response against the service
layer.

**Tool granularity:** confirmed directly — one MCP tool per service-layer
method, full 1:1 mapping, not composite/action-based tools. No dedicated
report/aggregate tools beyond the generic list/get tools — confirmed
directly that generic CRUD listing is enough for the reporting use case.

## Tool inventory

One `[McpServerToolType]` class per entity in a new
`src/ProductionBible.Api/Mcp/` folder, mirroring the existing
`Controllers/` folder's one-controller-per-entity structure. Each class
constructor-injects the matching `I*Service` (already registered in
`Program.cs`'s DI container for the REST API — no new registrations needed
beyond the MCP server itself). Every tool method is `async Task<T>`,
wrapping the async service call directly — no new business logic, no new
DTOs, no new validation beyond what the service layer already does.

**Parameter shape:** tool methods take flattened primitive parameters with
`[Description]` attributes, not the existing `Create*Request`/`Update*Request`
records passed as a single complex argument. Records have no per-property
`[Description]`, and flattened primitives give an LLM client a clearer,
self-documenting schema per the SDK's own guidance and examples (see
`docs/concepts/tools/tools.md`'s `Search(string query, int maxResults = 10)`
pattern). Each tool method constructs the request record internally from its
flattened parameters before calling the service.

**Naming convention:** `snake_case`, `<verb>_<entity>` or
`<verb>_<entity>s`, e.g. `list_projects`, `get_project`, `create_project`.

### `ProjectTools` — from `IProjectService`

- `list_projects()` → `GetAllAsync()`
- `get_project(int projectId)` → `GetByIdAsync(id)`
- `create_project(string name, string? description)` → `CreateAsync(new CreateProjectRequest(name, description))`
- `update_project(int projectId, string name, string? description)` → `UpdateAsync(id, new UpdateProjectRequest(name, description))`
- `delete_project(int projectId)` → `DeleteAsync(id)`

### `EpisodeTools` — from `IEpisodeService`

- `list_episodes(int projectId)` → `GetByProjectAsync(projectId)`
- `get_episode(int episodeId)` → `GetByIdAsync(id)`
- `create_episode(int projectId, string name, int orderIndex)` → `CreateAsync(projectId, new CreateEpisodeRequest(name, orderIndex))`
- `update_episode(int episodeId, string name, int orderIndex)` → `UpdateAsync(id, new UpdateEpisodeRequest(name, orderIndex))`
- `delete_episode(int episodeId)` → `DeleteAsync(id)`

### `BeatTools` — from `IBeatService`

- `list_beats(int episodeId)` → `GetByEpisodeAsync(episodeId)`
- `get_beat(int beatId)` → `GetByIdAsync(id)`
- `create_beat(int episodeId, string timecode, string purpose, int ordinal, int durationSeconds)` → `CreateAsync(episodeId, new CreateBeatRequest(timecode, purpose, ordinal, durationSeconds))`
- `update_beat(int beatId, string timecode, string purpose, int ordinal, int durationSeconds)` → `UpdateAsync(id, new UpdateBeatRequest(...))`
- `delete_beat(int beatId)` → `DeleteAsync(id)`
- `reorder_beats(int episodeId, int[] orderedBeatIds)` → `ReorderAsync(episodeId, orderedBeatIds)`

### `AssetTools` — from `IAssetService`

- `list_assets(int episodeId)` → `GetByEpisodeAsync(episodeId)`
- `get_asset(int assetId)` → `GetByIdAsync(id)`
- `create_asset(int episodeId, int assetTypeId, string code, string title, string? scriptText, string status, string? notes, int? sequenceNumber, int? targetLengthSeconds, int? phaseId, Dictionary<string,string> attributes, int[] beatIds)` → `CreateAsync(episodeId, new CreateAssetRequest(...))`
- `update_asset(int assetId, ...same fields as create...)` → `UpdateAsync(id, new UpdateAssetRequest(...))`
- `delete_asset(int assetId)` → `DeleteAsync(id)`
- `reorder_assets_within_phase(int phaseId, int[] orderedAssetIds)` → `ReorderWithinPhaseAsync(phaseId, orderedAssetIds)`
- `reorder_assets_within_beat(int beatId, int[] orderedAssetIds)` → `ReorderWithinBeatAsync(beatId, orderedAssetIds)`

(`create_asset`/`update_asset`'s exact flattened parameter list must match
`AssetDtos.cs`'s current `CreateAssetRequest`/`UpdateAssetRequest` field-for-
field at implementation time — read the file directly rather than trusting
this list, since Asset's DTO shape has changed twice already this project
per `CLAUDE.md`'s history, most recently gaining `OrderInPhase`.)

### `AssetTypeTools` — from `IAssetTypeService`

- `list_asset_types()` → `GetAllAsync()`
- `get_asset_type(int assetTypeId)` → `GetByIdAsync(id)`
- `create_asset_type(string name)` → `CreateAsync(new CreateAssetTypeRequest(name))`
- `delete_asset_type(int assetTypeId)` → `DeleteAsync(id)`

(No `update_asset_type` — `IAssetTypeService` has no `UpdateAsync`. Don't
add one; this spec covers parity with the service layer as it exists
today, not a new capability.)

### `PhaseTools` — from `IPhaseService`

- `list_phases(int projectId)` → `GetByProjectAsync(projectId)`
- `get_phase(int phaseId)` → `GetByIdAsync(id)`
- `create_phase(int projectId, string name, int orderIndex)` → `CreateAsync(projectId, new CreatePhaseRequest(name, orderIndex))`
- `update_phase(int phaseId, string name, int orderIndex)` → `UpdateAsync(id, new UpdatePhaseRequest(...))`
- `delete_phase(int phaseId)` → `DeleteAsync(id)`
- `reorder_phases(int projectId, int[] orderedPhaseIds)` → `ReorderAsync(projectId, orderedPhaseIds)`

**Total: 33 tools** (5 + 5 + 6 + 7 + 4 + 6), one per service method, no
more and no fewer.

## Return values and error handling

Tool methods return the same DTOs the REST API returns
(`ProjectDto`, `EpisodeDto`, etc. — already JSON-serializable, no new
mapping needed). The SDK serializes a returned object to the tool's result
content automatically (see SDK docs on tool return handling).

A `Get*`/`Update*` method that returns `null` (not found) returns `null`
from the tool too — the SDK's JSON schema marks it nullable, and an LLM
client sees an explicit "not found" rather than a thrown exception. A
`Delete*` method returning `false` (not found) likewise returns `false`
rather than throwing. This matches the REST API's own not-found handling
(404 for missing resources) in spirit, translated to MCP's request/response
shape rather than HTTP status codes — no new error-handling scheme is
introduced.

No authentication or authorization — matches the REST API and the rest of
this trusted-LAN app. No new logging beyond ASP.NET Core's existing default
request logging picking up `/mcp` requests like any other route.

## Testing

New test file(s) in the existing `tests/ProductionBible.Api.Tests` project
(already has `TestWebApplicationFactory`, an `IClassFixture<>`-based
`WebApplicationFactory<Program>` that swaps the real SQLite database for a
unique temp-file SQLite database per test run — see
`tests/ProductionBible.Api.Tests/TestWebApplicationFactory.cs`). MCP tool
tests reuse this same factory rather than introducing a second test
fixture.

Coverage:
- Tool registration: the MCP server actually lists all 33 tools (a
  `tools/list` assertion via `McpClient.ListToolsAsync()`). The
  implementation plan's first task determines the concrete client/transport
  wiring by reading the installed `ModelContextProtocol` package's actual
  API surface directly (its `HttpClientTransport`/`HttpClientTransportOptions`
  types, checked for a constructor or option that accepts an existing
  `HttpClient` — `TestWebApplicationFactory.CreateClient()` already returns
  one wired to the in-process test server) rather than guessing the shape
  here. Reuse `TestWebApplicationFactory` — don't introduce a second test
  fixture or a parallel test-hosting mechanism; point the MCP client at the
  existing factory's `/mcp` route.
- At least one full round-trip test per entity (create via the MCP tool,
  then get/list via another MCP tool, confirming the same values the REST
  API's own equivalent test — see `ProjectsApiTests.cs`'s
  `Post_then_get_round_trips_a_project_through_the_real_http_pipeline` — asserts
  for its REST equivalent), so both surfaces are proven to share the same
  underlying data rather than just structurally similar test coverage.
- Not-found handling: one test confirming a `get_*`/`delete_*` tool call
  against a nonexistent id returns the documented null/false shape rather
  than throwing.
- The two most state-changing tools (`reorder_beats`, `reorder_assets_within_phase`
  or similar) get at least one test each, since reorder logic has been the
  single most bug-prone area of this codebase across both roadmap tracks
  (see `CLAUDE.md`'s history and GitHub issues #18–#26).

No new frontend work — this phase has no Angular component, since MCP
clients are not the web browser.

## Deferred / explicitly out of scope

- Authentication/authorization on the MCP endpoint — matches the rest of
  the app's current no-auth state; revisit only if the whole app's auth
  story changes (Phase 1's own spec already deferred auth generally, no
  target phase assigned).
- Composite/action-based tools, and dedicated report/aggregate tools
  (e.g. "list unshot assets") — confirmed directly as out of scope for
  this phase; the generic list/get tools are enough for the stated
  reporting use case. Worth revisiting later if a concrete reporting need
  the generic tools can't answer shows up.
- stdio transport / a separately-launchable MCP entry point — confirmed
  directly as not needed; every MCP client reaches this server the same
  way it reaches the REST API, over the LAN via HTTP.
- Any change to the REST API or Angular frontend — this phase adds a
  second protocol surface over the existing service layer, it does not
  change the first one.
