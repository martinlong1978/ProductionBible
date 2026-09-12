# MCP Server Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an MCP server, in-process with the existing REST API, exposing all 33 service-layer methods as MCP tools with full CRUD parity.

**Architecture:** `ModelContextProtocol.AspNetCore` hosted inside `ProductionBible.Api`'s existing Kestrel process, mounted at `/mcp` via Streamable HTTP (stateless mode). One `[McpServerToolType]` class per entity in a new `Mcp/` folder, each constructor-injecting the matching already-registered `I*Service`. Tests reuse the existing `TestWebApplicationFactory` and a new small MCP test-client helper (built in Task 1) rather than a second test-hosting mechanism.

**Tech Stack:** ASP.NET Core (.NET 10), `ModelContextProtocol.AspNetCore` 2.2.0 (latest stable as of this plan), xUnit.

**Spec:** `docs/superpowers/specs/2026-09-12-mcp-server-design.md`

## Global Constraints

- No REST API or Angular frontend changes — this plan only adds a second protocol surface over the existing service layer.
- No authentication — matches the REST API's existing no-auth LAN-trust model.
- One MCP tool per service-layer method, full 1:1 mapping — no composite/action tools, no dedicated report/aggregate tools.
- Tool naming: `snake_case`, `<verb>_<entity>` (singular for get/create/update/delete, plural for list), e.g. `list_projects`, `get_project`.
- Tool method parameters are flattened primitives with `[Description]` attributes, not the raw `Create*Request`/`Update*Request` records passed as a single object.
- Every tool method wraps its service call directly — no new business logic, no new DTOs, no new validation beyond what the service layer already does.
- `Asset`'s create/update tool parameter list must be read directly from `src/ProductionBible.Application/Dtos/AssetDtos.cs` at implementation time, not copied from this plan without checking — Asset's DTO shape has changed twice already this project.

---

## File Structure

- Create: `src/ProductionBible.Api/Mcp/ProjectTools.cs`, `EpisodeTools.cs`, `PhaseTools.cs`, `BeatTools.cs`, `AssetTypeTools.cs`, `AssetTools.cs` — one `[McpServerToolType]` class per entity.
- Modify: `src/ProductionBible.Api/Program.cs` — MCP server registration + `/mcp` mapping.
- Modify: `src/ProductionBible.Api/ProductionBible.Api.csproj` — new package reference.
- Modify: `tests/ProductionBible.Api.Tests/ProductionBible.Api.Tests.csproj` — new package reference (MCP client).
- Create: `tests/ProductionBible.Api.Tests/Mcp/McpTestClient.cs` — shared test helper (connect + call + deserialize).
- Create: `tests/ProductionBible.Api.Tests/Mcp/ProjectToolsTests.cs`, `EpisodeToolsTests.cs`, `PhaseToolsTests.cs`, `BeatToolsTests.cs`, `AssetTypeToolsTests.cs`, `AssetToolsTests.cs`.

---

## Task 1: MCP server wiring + shared test-client helper

**Files:**
- Modify: `src/ProductionBible.Api/ProductionBible.Api.csproj`
- Modify: `src/ProductionBible.Api/Program.cs`
- Modify: `tests/ProductionBible.Api.Tests/ProductionBible.Api.Tests.csproj`
- Create: `tests/ProductionBible.Api.Tests/Mcp/McpTestClient.cs`
- Create: `tests/ProductionBible.Api.Tests/Mcp/McpServerWiringTests.cs`

**Interfaces:**
- Produces: `McpTestClient.ConnectAsync(TestWebApplicationFactory factory) : Task<McpClient>` and `McpTestClient.CallAndReadJsonAsync<T>(McpClient client, string toolName, Dictionary<string, object?> arguments) : Task<T?>` — consumed by every later task's tests. `TestWebApplicationFactory` itself (`tests/ProductionBible.Api.Tests/TestWebApplicationFactory.cs`) already exists and is unchanged by this task.

This task's real job is nailing down the exact `ModelContextProtocol` client API against the version actually installed — the SDK's documentation examples connect over a real network endpoint or stdio, not against an in-process `WebApplicationFactory<Program>`'s test server, so the exact constructor/option for routing an `HttpClientTransport` through an existing `HttpClient` (rather than a real socket) is determined here by reading the installed package's actual public API, not guessed.

- [ ] **Step 1: Add the MCP packages**

Add to `src/ProductionBible.Api/ProductionBible.Api.csproj`'s existing `<ItemGroup>` with the OpenApi package reference:

```xml
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="2.2.0" />
```

Add to `tests/ProductionBible.Api.Tests/ProductionBible.Api.Tests.csproj`'s existing `<ItemGroup>` with the other package references:

```xml
<PackageReference Include="ModelContextProtocol" Version="2.2.0" />
```

Run: `dotnet restore ProductionBible.sln`

Expected: restores cleanly. If `ModelContextProtocol` (bare) doesn't expose `ModelContextProtocol.Client`'s `McpClient`/`HttpClientTransport` types when you reach Step 4, add `ModelContextProtocol.Core` (same version) to the test project too and retry — the SDK splits functionality across these three packages and the exact split can shift between versions.

- [ ] **Step 2: Wire the MCP server into `Program.cs`**

In `src/ProductionBible.Api/Program.cs`, add to the `using` block at the top:

```csharp
using ModelContextProtocol.AspNetCore;
```

After the existing `builder.Services.AddScoped<IAssetService, AssetService>();` line and before `var app = builder.Build();`, add:

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.SessionMode = HttpServerSessionMode.Stateless;
    })
    .WithToolsFromAssembly();
```

After the existing `app.MapControllers();` line, add:

```csharp
app.MapMcp("/mcp");
```

(Leave `app.MapFallbackToFile("index.html");` after this — `/mcp` is a specific route match and won't be shadowed by the SPA fallback.)

- [ ] **Step 3: Write the failing wiring test**

Create `tests/ProductionBible.Api.Tests/Mcp/McpServerWiringTests.cs`:

```csharp
namespace ProductionBible.Api.Tests.Mcp;

public class McpServerWiringTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public McpServerWiringTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Mcp_endpoint_is_reachable_and_lists_no_tools_yet()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);

        var tools = await client.ListToolsAsync();

        Assert.Empty(tools);
    }
}
```

Run: `dotnet test tests/ProductionBible.Api.Tests --filter McpServerWiringTests`

Expected: FAIL — `McpTestClient` doesn't exist yet.

- [ ] **Step 4: Implement `McpTestClient`**

Create `tests/ProductionBible.Api.Tests/Mcp/McpTestClient.cs`. Start from this shape and adjust the transport-construction line to match whatever constructor/options the installed `ModelContextProtocol` package's `HttpClientTransport` actually exposes for supplying an existing `HttpClient` (check its constructors directly — via your IDE's/editor's go-to-definition, or by writing a call with placeholder arguments and reading the compiler's candidate-overload list — rather than assuming the shape below is exact):

```csharp
using ModelContextProtocol.Client;

namespace ProductionBible.Api.Tests.Mcp;

public static class McpTestClient
{
    public static async Task<McpClient> ConnectAsync(TestWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        httpClient.BaseAddress = new Uri(httpClient.BaseAddress!, "/mcp");

        // The exact HttpClientTransport constructor/options for supplying an
        // existing HttpClient (so requests go through the in-memory test server
        // rather than a real socket) must be confirmed against the installed
        // package version — inspect HttpClientTransport's actual constructors.
        var transport = new HttpClientTransport(httpClient);

        return await McpClient.CreateAsync(transport);
    }

    public static async Task<T?> CallAndReadJsonAsync<T>(
        McpClient client, string toolName, Dictionary<string, object?> arguments)
    {
        var result = await client.CallToolAsync(toolName, arguments);

        var textBlock = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        if (textBlock is null) return default;

        return System.Text.Json.JsonSerializer.Deserialize<T>(
            textBlock.Text, System.Text.Json.JsonSerializerOptions.Web);
    }
}
```

If a tool's return value doesn't come back as a `TextContentBlock` of JSON text under this SDK version (e.g. it comes back as structured content instead), adjust `CallAndReadJsonAsync` to read whichever `CallToolResult` member actually carries the JSON payload — confirm by adding a temporary `Console.WriteLine` or debugger inspection of the raw `result` in Step 5's test run, then remove it once the real accessor is confirmed. Whatever the actual mechanism turns out to be, `CallAndReadJsonAsync<T>`'s signature (name, parameters, return type) must not change, since every later task's tests call it exactly as declared above.

- [ ] **Step 5: Run the wiring test until it passes**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter McpServerWiringTests`

Expected: PASS once `McpTestClient.ConnectAsync` compiles against the real SDK and the MCP handshake completes. Iterate on Step 4 using the actual compiler/runtime errors as feedback until this passes — do not proceed to Task 2 with a guessed API that doesn't actually compile and run.

- [ ] **Step 6: Run the full backend suite**

Run: `dotnet test ProductionBible.sln`

Expected: PASS, including the pre-existing REST API tests (`ProjectsApiTests` etc.) — confirms adding the MCP server didn't disturb the existing HTTP pipeline.

- [ ] **Step 7: Commit**

```bash
git add src/ProductionBible.Api/ProductionBible.Api.csproj src/ProductionBible.Api/Program.cs \
        tests/ProductionBible.Api.Tests/ProductionBible.Api.Tests.csproj \
        tests/ProductionBible.Api.Tests/Mcp/McpTestClient.cs \
        tests/ProductionBible.Api.Tests/Mcp/McpServerWiringTests.cs
git commit -m "Wire an MCP server into the API process, mounted at /mcp

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 2: `ProjectTools`

**Files:**
- Create: `src/ProductionBible.Api/Mcp/ProjectTools.cs`
- Create: `tests/ProductionBible.Api.Tests/Mcp/ProjectToolsTests.cs`

**Interfaces:**
- Consumes: `McpTestClient.ConnectAsync`/`CallAndReadJsonAsync<T>` (Task 1). `IProjectService` (existing, `src/ProductionBible.Application/Services/IProjectService.cs`).
- Produces: 5 MCP tools — `list_projects`, `get_project`, `create_project`, `update_project`, `delete_project` — no other task depends on these directly, but every later entity's tools operate on data scoped under a project, so this task's `create_project` tool is what later tasks' tests use to set up their own fixture data.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProductionBible.Api.Tests/Mcp/ProjectToolsTests.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests.Mcp;

public class ProjectToolsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ProjectToolsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_then_get_then_list_round_trips_a_project()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);

        var created = await McpTestClient.CallAndReadJsonAsync<ProjectDto>(client, "create_project",
            new() { ["name"] = "HalfNut ELS", ["description"] = "The lathe series" });

        Assert.NotNull(created);
        Assert.Equal("HalfNut ELS", created!.Name);

        var fetched = await McpTestClient.CallAndReadJsonAsync<ProjectDto>(client, "get_project",
            new() { ["projectId"] = created.Id });

        Assert.NotNull(fetched);
        Assert.Equal("HalfNut ELS", fetched!.Name);

        var listed = await McpTestClient.CallAndReadJsonAsync<List<ProjectDto>>(client, "list_projects", new());

        Assert.Contains(listed!, p => p.Id == created.Id);
    }

    [Fact]
    public async Task Update_changes_the_projects_fields()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var created = await McpTestClient.CallAndReadJsonAsync<ProjectDto>(client, "create_project",
            new() { ["name"] = "Original", ["description"] = (string?)null });

        var updated = await McpTestClient.CallAndReadJsonAsync<ProjectDto>(client, "update_project",
            new() { ["projectId"] = created!.Id, ["name"] = "Renamed", ["description"] = "Now with a description" });

        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal("Now with a description", updated.Description);
    }

    [Fact]
    public async Task Delete_removes_the_project_and_a_second_get_returns_null()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var created = await McpTestClient.CallAndReadJsonAsync<ProjectDto>(client, "create_project",
            new() { ["name"] = "To delete", ["description"] = (string?)null });

        var deleted = await McpTestClient.CallAndReadJsonAsync<bool>(client, "delete_project",
            new() { ["projectId"] = created!.Id });
        Assert.True(deleted);

        var fetched = await McpTestClient.CallAndReadJsonAsync<ProjectDto?>(client, "get_project",
            new() { ["projectId"] = created.Id });
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Get_unknown_project_returns_null_not_an_error()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);

        var fetched = await McpTestClient.CallAndReadJsonAsync<ProjectDto?>(client, "get_project",
            new() { ["projectId"] = 9_999_999 });

        Assert.Null(fetched);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter ProjectToolsTests`

Expected: FAIL — `create_project`/`get_project`/etc. tools don't exist, so `CallToolAsync` throws or returns an error result.

- [ ] **Step 3: Implement `ProjectTools`**

Create `src/ProductionBible.Api/Mcp/ProjectTools.cs`:

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Mcp;

[McpServerToolType]
public class ProjectTools(IProjectService projectService)
{
    [McpServerTool(Name = "list_projects"), Description("Lists every project.")]
    public Task<IReadOnlyList<ProjectDto>> ListProjects() => projectService.GetAllAsync();

    [McpServerTool(Name = "get_project"), Description("Gets a single project by id, or null if it doesn't exist.")]
    public Task<ProjectDto?> GetProject(
        [Description("The project's id")] int projectId) => projectService.GetByIdAsync(projectId);

    [McpServerTool(Name = "create_project"), Description("Creates a new project.")]
    public Task<ProjectDto> CreateProject(
        [Description("The project's name")] string name,
        [Description("An optional free-text description")] string? description) =>
        projectService.CreateAsync(new CreateProjectRequest(name, description));

    [McpServerTool(Name = "update_project"), Description("Updates an existing project's name and description.")]
    public Task<ProjectDto?> UpdateProject(
        [Description("The project's id")] int projectId,
        [Description("The project's name")] string name,
        [Description("An optional free-text description")] string? description) =>
        projectService.UpdateAsync(projectId, new UpdateProjectRequest(name, description));

    [McpServerTool(Name = "delete_project"), Description("Deletes a project. Returns false if it didn't exist.")]
    public Task<bool> DeleteProject(
        [Description("The project's id")] int projectId) => projectService.DeleteAsync(projectId);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter ProjectToolsTests`

Expected: PASS, all 4 tests.

- [ ] **Step 5: Run the full backend suite**

Run: `dotnet test ProductionBible.sln`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/ProductionBible.Api/Mcp/ProjectTools.cs tests/ProductionBible.Api.Tests/Mcp/ProjectToolsTests.cs
git commit -m "Add MCP tools for Project (list/get/create/update/delete)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 3: `EpisodeTools`

**Files:**
- Create: `src/ProductionBible.Api/Mcp/EpisodeTools.cs`
- Create: `tests/ProductionBible.Api.Tests/Mcp/EpisodeToolsTests.cs`

**Interfaces:**
- Consumes: `McpTestClient` (Task 1), `create_project` tool (Task 2, to create a parent project fixture), `IEpisodeService` (existing).
- Produces: `list_episodes`, `get_episode`, `create_episode`, `update_episode`, `delete_episode`.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProductionBible.Api.Tests/Mcp/EpisodeToolsTests.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests.Mcp;

public class EpisodeToolsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public EpisodeToolsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CreateProjectAsync(ModelContextProtocol.Client.McpClient client)
    {
        var project = await McpTestClient.CallAndReadJsonAsync<ProjectDto>(client, "create_project",
            new() { ["name"] = "Episode test project", ["description"] = (string?)null });
        return project!.Id;
    }

    [Fact]
    public async Task Create_then_get_then_list_round_trips_an_episode()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var projectId = await CreateProjectAsync(client);

        var created = await McpTestClient.CallAndReadJsonAsync<EpisodeDto>(client, "create_episode",
            new() { ["projectId"] = projectId, ["name"] = "EP1", ["orderIndex"] = 1 });

        Assert.Equal("EP1", created!.Name);

        var fetched = await McpTestClient.CallAndReadJsonAsync<EpisodeDto>(client, "get_episode",
            new() { ["episodeId"] = created.Id });
        Assert.Equal("EP1", fetched!.Name);

        var listed = await McpTestClient.CallAndReadJsonAsync<List<EpisodeDto>>(client, "list_episodes",
            new() { ["projectId"] = projectId });
        Assert.Contains(listed!, e => e.Id == created.Id);
    }

    [Fact]
    public async Task Update_changes_the_episodes_fields()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var projectId = await CreateProjectAsync(client);
        var created = await McpTestClient.CallAndReadJsonAsync<EpisodeDto>(client, "create_episode",
            new() { ["projectId"] = projectId, ["name"] = "Original", ["orderIndex"] = 0 });

        var updated = await McpTestClient.CallAndReadJsonAsync<EpisodeDto>(client, "update_episode",
            new() { ["episodeId"] = created!.Id, ["name"] = "Renamed", ["orderIndex"] = 5 });

        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal(5, updated.OrderIndex);
    }

    [Fact]
    public async Task Delete_removes_the_episode()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var projectId = await CreateProjectAsync(client);
        var created = await McpTestClient.CallAndReadJsonAsync<EpisodeDto>(client, "create_episode",
            new() { ["projectId"] = projectId, ["name"] = "To delete", ["orderIndex"] = 0 });

        var deleted = await McpTestClient.CallAndReadJsonAsync<bool>(client, "delete_episode",
            new() { ["episodeId"] = created!.Id });
        Assert.True(deleted);

        var fetched = await McpTestClient.CallAndReadJsonAsync<EpisodeDto?>(client, "get_episode",
            new() { ["episodeId"] = created.Id });
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Get_unknown_episode_returns_null()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);

        var fetched = await McpTestClient.CallAndReadJsonAsync<EpisodeDto?>(client, "get_episode",
            new() { ["episodeId"] = 9_999_999 });

        Assert.Null(fetched);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter EpisodeToolsTests`

Expected: FAIL.

- [ ] **Step 3: Implement `EpisodeTools`**

Create `src/ProductionBible.Api/Mcp/EpisodeTools.cs`:

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Mcp;

[McpServerToolType]
public class EpisodeTools(IEpisodeService episodeService)
{
    [McpServerTool(Name = "list_episodes"), Description("Lists every episode in a project.")]
    public Task<IReadOnlyList<EpisodeDto>> ListEpisodes(
        [Description("The parent project's id")] int projectId) => episodeService.GetByProjectAsync(projectId);

    [McpServerTool(Name = "get_episode"), Description("Gets a single episode by id, or null if it doesn't exist.")]
    public Task<EpisodeDto?> GetEpisode(
        [Description("The episode's id")] int episodeId) => episodeService.GetByIdAsync(episodeId);

    [McpServerTool(Name = "create_episode"), Description("Creates a new episode in a project.")]
    public Task<EpisodeDto> CreateEpisode(
        [Description("The parent project's id")] int projectId,
        [Description("The episode's name, e.g. 'EP1'")] string name,
        [Description("Display order within the project, ascending")] int orderIndex) =>
        episodeService.CreateAsync(projectId, new CreateEpisodeRequest(name, orderIndex));

    [McpServerTool(Name = "update_episode"), Description("Updates an existing episode's name and order.")]
    public Task<EpisodeDto?> UpdateEpisode(
        [Description("The episode's id")] int episodeId,
        [Description("The episode's name")] string name,
        [Description("Display order within the project, ascending")] int orderIndex) =>
        episodeService.UpdateAsync(episodeId, new UpdateEpisodeRequest(name, orderIndex));

    [McpServerTool(Name = "delete_episode"), Description("Deletes an episode. Returns false if it didn't exist.")]
    public Task<bool> DeleteEpisode(
        [Description("The episode's id")] int episodeId) => episodeService.DeleteAsync(episodeId);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter EpisodeToolsTests`

Expected: PASS, all 4 tests.

- [ ] **Step 5: Run the full backend suite**

Run: `dotnet test ProductionBible.sln`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/ProductionBible.Api/Mcp/EpisodeTools.cs tests/ProductionBible.Api.Tests/Mcp/EpisodeToolsTests.cs
git commit -m "Add MCP tools for Episode (list/get/create/update/delete)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 4: `AssetTypeTools`

**Files:**
- Create: `src/ProductionBible.Api/Mcp/AssetTypeTools.cs`
- Create: `tests/ProductionBible.Api.Tests/Mcp/AssetTypeToolsTests.cs`

**Interfaces:**
- Consumes: `McpTestClient` (Task 1), `IAssetTypeService` (existing).
- Produces: `list_asset_types`, `get_asset_type`, `create_asset_type`, `delete_asset_type`. No `update_asset_type` — `IAssetTypeService` has no update method; do not add one.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProductionBible.Api.Tests/Mcp/AssetTypeToolsTests.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests.Mcp;

public class AssetTypeToolsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AssetTypeToolsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_then_get_then_list_round_trips_an_asset_type()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);

        var created = await McpTestClient.CallAndReadJsonAsync<AssetTypeDto>(client, "create_asset_type",
            new() { ["name"] = "Shot" });

        Assert.Equal("Shot", created!.Name);

        var fetched = await McpTestClient.CallAndReadJsonAsync<AssetTypeDto>(client, "get_asset_type",
            new() { ["assetTypeId"] = created.Id });
        Assert.Equal("Shot", fetched!.Name);

        var listed = await McpTestClient.CallAndReadJsonAsync<List<AssetTypeDto>>(client, "list_asset_types", new());
        Assert.Contains(listed!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task Delete_removes_the_asset_type()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var created = await McpTestClient.CallAndReadJsonAsync<AssetTypeDto>(client, "create_asset_type",
            new() { ["name"] = "To delete" });

        var deleted = await McpTestClient.CallAndReadJsonAsync<bool>(client, "delete_asset_type",
            new() { ["assetTypeId"] = created!.Id });
        Assert.True(deleted);

        var fetched = await McpTestClient.CallAndReadJsonAsync<AssetTypeDto?>(client, "get_asset_type",
            new() { ["assetTypeId"] = created.Id });
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Get_unknown_asset_type_returns_null()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);

        var fetched = await McpTestClient.CallAndReadJsonAsync<AssetTypeDto?>(client, "get_asset_type",
            new() { ["assetTypeId"] = 9_999_999 });

        Assert.Null(fetched);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter AssetTypeToolsTests`

Expected: FAIL.

- [ ] **Step 3: Implement `AssetTypeTools`**

Create `src/ProductionBible.Api/Mcp/AssetTypeTools.cs`:

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Mcp;

[McpServerToolType]
public class AssetTypeTools(IAssetTypeService assetTypeService)
{
    [McpServerTool(Name = "list_asset_types"), Description("Lists every asset type (e.g. Shot, VoiceOver, Graphic).")]
    public Task<IReadOnlyList<AssetTypeDto>> ListAssetTypes() => assetTypeService.GetAllAsync();

    [McpServerTool(Name = "get_asset_type"), Description("Gets a single asset type by id, or null if it doesn't exist.")]
    public Task<AssetTypeDto?> GetAssetType(
        [Description("The asset type's id")] int assetTypeId) => assetTypeService.GetByIdAsync(assetTypeId);

    [McpServerTool(Name = "create_asset_type"), Description("Creates a new asset type.")]
    public Task<AssetTypeDto> CreateAssetType(
        [Description("The asset type's name, e.g. 'Shot' or 'VoiceOver'")] string name) =>
        assetTypeService.CreateAsync(new CreateAssetTypeRequest(name));

    [McpServerTool(Name = "delete_asset_type"), Description("Deletes an asset type. Returns false if it didn't exist.")]
    public Task<bool> DeleteAssetType(
        [Description("The asset type's id")] int assetTypeId) => assetTypeService.DeleteAsync(assetTypeId);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter AssetTypeToolsTests`

Expected: PASS, all 3 tests.

- [ ] **Step 5: Run the full backend suite**

Run: `dotnet test ProductionBible.sln`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/ProductionBible.Api/Mcp/AssetTypeTools.cs tests/ProductionBible.Api.Tests/Mcp/AssetTypeToolsTests.cs
git commit -m "Add MCP tools for AssetType (list/get/create/delete)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 5: `PhaseTools`

**Files:**
- Create: `src/ProductionBible.Api/Mcp/PhaseTools.cs`
- Create: `tests/ProductionBible.Api.Tests/Mcp/PhaseToolsTests.cs`

**Interfaces:**
- Consumes: `McpTestClient` (Task 1), `create_project` tool (Task 2), `IPhaseService` (existing).
- Produces: `list_phases`, `get_phase`, `create_phase`, `update_phase`, `delete_phase`, `reorder_phases`.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProductionBible.Api.Tests/Mcp/PhaseToolsTests.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests.Mcp;

public class PhaseToolsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PhaseToolsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CreateProjectAsync(ModelContextProtocol.Client.McpClient client)
    {
        var project = await McpTestClient.CallAndReadJsonAsync<ProjectDto>(client, "create_project",
            new() { ["name"] = "Phase test project", ["description"] = (string?)null });
        return project!.Id;
    }

    [Fact]
    public async Task Create_then_get_then_list_round_trips_a_phase()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var projectId = await CreateProjectAsync(client);

        var created = await McpTestClient.CallAndReadJsonAsync<PhaseDto>(client, "create_phase",
            new() { ["projectId"] = projectId, ["name"] = "Phase 1", ["orderIndex"] = 0 });

        Assert.Equal("Phase 1", created!.Name);

        var fetched = await McpTestClient.CallAndReadJsonAsync<PhaseDto>(client, "get_phase",
            new() { ["phaseId"] = created.Id });
        Assert.Equal("Phase 1", fetched!.Name);

        var listed = await McpTestClient.CallAndReadJsonAsync<List<PhaseDto>>(client, "list_phases",
            new() { ["projectId"] = projectId });
        Assert.Contains(listed!, p => p.Id == created.Id);
    }

    [Fact]
    public async Task Update_changes_the_phases_fields()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var projectId = await CreateProjectAsync(client);
        var created = await McpTestClient.CallAndReadJsonAsync<PhaseDto>(client, "create_phase",
            new() { ["projectId"] = projectId, ["name"] = "Original", ["orderIndex"] = 0 });

        var updated = await McpTestClient.CallAndReadJsonAsync<PhaseDto>(client, "update_phase",
            new() { ["phaseId"] = created!.Id, ["name"] = "Renamed", ["orderIndex"] = 3 });

        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal(3, updated.OrderIndex);
    }

    [Fact]
    public async Task Delete_removes_the_phase()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var projectId = await CreateProjectAsync(client);
        var created = await McpTestClient.CallAndReadJsonAsync<PhaseDto>(client, "create_phase",
            new() { ["projectId"] = projectId, ["name"] = "To delete", ["orderIndex"] = 0 });

        var deleted = await McpTestClient.CallAndReadJsonAsync<bool>(client, "delete_phase",
            new() { ["phaseId"] = created!.Id });
        Assert.True(deleted);

        var fetched = await McpTestClient.CallAndReadJsonAsync<PhaseDto?>(client, "get_phase",
            new() { ["phaseId"] = created.Id });
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Reorder_phases_persists_the_new_order()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var projectId = await CreateProjectAsync(client);
        var first = await McpTestClient.CallAndReadJsonAsync<PhaseDto>(client, "create_phase",
            new() { ["projectId"] = projectId, ["name"] = "First", ["orderIndex"] = 0 });
        var second = await McpTestClient.CallAndReadJsonAsync<PhaseDto>(client, "create_phase",
            new() { ["projectId"] = projectId, ["name"] = "Second", ["orderIndex"] = 1 });

        var reordered = await McpTestClient.CallAndReadJsonAsync<bool>(client, "reorder_phases",
            new() { ["projectId"] = projectId, ["orderedPhaseIds"] = new[] { second!.Id, first!.Id } });
        Assert.True(reordered);

        var listed = await McpTestClient.CallAndReadJsonAsync<List<PhaseDto>>(client, "list_phases",
            new() { ["projectId"] = projectId });
        var ordered = listed!.OrderBy(p => p.OrderIndex).ToList();
        Assert.Equal(second.Id, ordered[0].Id);
        Assert.Equal(first.Id, ordered[1].Id);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter PhaseToolsTests`

Expected: FAIL.

- [ ] **Step 3: Implement `PhaseTools`**

Create `src/ProductionBible.Api/Mcp/PhaseTools.cs`:

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Mcp;

[McpServerToolType]
public class PhaseTools(IPhaseService phaseService)
{
    [McpServerTool(Name = "list_phases"), Description("Lists every phase in a project, in display order.")]
    public Task<IReadOnlyList<PhaseDto>> ListPhases(
        [Description("The parent project's id")] int projectId) => phaseService.GetByProjectAsync(projectId);

    [McpServerTool(Name = "get_phase"), Description("Gets a single phase by id, or null if it doesn't exist.")]
    public Task<PhaseDto?> GetPhase(
        [Description("The phase's id")] int phaseId) => phaseService.GetByIdAsync(phaseId);

    [McpServerTool(Name = "create_phase"), Description("Creates a new phase in a project.")]
    public Task<PhaseDto> CreatePhase(
        [Description("The parent project's id")] int projectId,
        [Description("The phase's name")] string name,
        [Description("Display order within the project, ascending")] int orderIndex) =>
        phaseService.CreateAsync(projectId, new CreatePhaseRequest(name, orderIndex));

    [McpServerTool(Name = "update_phase"), Description("Updates an existing phase's name and order.")]
    public Task<PhaseDto?> UpdatePhase(
        [Description("The phase's id")] int phaseId,
        [Description("The phase's name")] string name,
        [Description("Display order within the project, ascending")] int orderIndex) =>
        phaseService.UpdateAsync(phaseId, new UpdatePhaseRequest(name, orderIndex));

    [McpServerTool(Name = "delete_phase"), Description("Deletes a phase. Returns false if it didn't exist.")]
    public Task<bool> DeletePhase(
        [Description("The phase's id")] int phaseId) => phaseService.DeleteAsync(phaseId);

    [McpServerTool(Name = "reorder_phases"), Description("Reorders every phase in a project to match the given id sequence.")]
    public Task<bool> ReorderPhases(
        [Description("The parent project's id")] int projectId,
        [Description("Every phase id in the project, in the desired new order")] int[] orderedPhaseIds) =>
        phaseService.ReorderAsync(projectId, orderedPhaseIds);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter PhaseToolsTests`

Expected: PASS, all 4 tests.

- [ ] **Step 5: Run the full backend suite**

Run: `dotnet test ProductionBible.sln`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/ProductionBible.Api/Mcp/PhaseTools.cs tests/ProductionBible.Api.Tests/Mcp/PhaseToolsTests.cs
git commit -m "Add MCP tools for Phase (list/get/create/update/delete/reorder)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 6: `BeatTools`

**Files:**
- Create: `src/ProductionBible.Api/Mcp/BeatTools.cs`
- Create: `tests/ProductionBible.Api.Tests/Mcp/BeatToolsTests.cs`

**Interfaces:**
- Consumes: `McpTestClient` (Task 1), `create_project`/`create_episode` tools (Tasks 2-3), `IBeatService` (existing).
- Produces: `list_beats`, `get_beat`, `create_beat`, `update_beat`, `delete_beat`, `reorder_beats`.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProductionBible.Api.Tests/Mcp/BeatToolsTests.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests.Mcp;

public class BeatToolsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BeatToolsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CreateEpisodeAsync(ModelContextProtocol.Client.McpClient client)
    {
        var project = await McpTestClient.CallAndReadJsonAsync<ProjectDto>(client, "create_project",
            new() { ["name"] = "Beat test project", ["description"] = (string?)null });
        var episode = await McpTestClient.CallAndReadJsonAsync<EpisodeDto>(client, "create_episode",
            new() { ["projectId"] = project!.Id, ["name"] = "EP1", ["orderIndex"] = 0 });
        return episode!.Id;
    }

    [Fact]
    public async Task Create_then_get_then_list_round_trips_a_beat()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var episodeId = await CreateEpisodeAsync(client);

        var created = await McpTestClient.CallAndReadJsonAsync<BeatDto>(client, "create_beat",
            new()
            {
                ["episodeId"] = episodeId, ["timecode"] = "00:00", ["purpose"] = "Cold open",
                ["ordinal"] = 0, ["durationSeconds"] = 30,
            });

        Assert.Equal("Cold open", created!.Purpose);

        var fetched = await McpTestClient.CallAndReadJsonAsync<BeatDto>(client, "get_beat",
            new() { ["beatId"] = created.Id });
        Assert.Equal("Cold open", fetched!.Purpose);

        var listed = await McpTestClient.CallAndReadJsonAsync<List<BeatDto>>(client, "list_beats",
            new() { ["episodeId"] = episodeId });
        Assert.Contains(listed!, b => b.Id == created.Id);
    }

    [Fact]
    public async Task Update_changes_the_beats_fields()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var episodeId = await CreateEpisodeAsync(client);
        var created = await McpTestClient.CallAndReadJsonAsync<BeatDto>(client, "create_beat",
            new()
            {
                ["episodeId"] = episodeId, ["timecode"] = "00:00", ["purpose"] = "Original",
                ["ordinal"] = 0, ["durationSeconds"] = 30,
            });

        var updated = await McpTestClient.CallAndReadJsonAsync<BeatDto>(client, "update_beat",
            new()
            {
                ["beatId"] = created!.Id, ["timecode"] = "01:00", ["purpose"] = "Renamed",
                ["ordinal"] = 1, ["durationSeconds"] = 45,
            });

        Assert.Equal("Renamed", updated!.Purpose);
        Assert.Equal(45, updated.DurationSeconds);
    }

    [Fact]
    public async Task Delete_removes_the_beat()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var episodeId = await CreateEpisodeAsync(client);
        var created = await McpTestClient.CallAndReadJsonAsync<BeatDto>(client, "create_beat",
            new()
            {
                ["episodeId"] = episodeId, ["timecode"] = "00:00", ["purpose"] = "To delete",
                ["ordinal"] = 0, ["durationSeconds"] = 30,
            });

        var deleted = await McpTestClient.CallAndReadJsonAsync<bool>(client, "delete_beat",
            new() { ["beatId"] = created!.Id });
        Assert.True(deleted);

        var fetched = await McpTestClient.CallAndReadJsonAsync<BeatDto?>(client, "get_beat",
            new() { ["beatId"] = created.Id });
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Reorder_beats_persists_the_new_order()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var episodeId = await CreateEpisodeAsync(client);
        var first = await McpTestClient.CallAndReadJsonAsync<BeatDto>(client, "create_beat",
            new()
            {
                ["episodeId"] = episodeId, ["timecode"] = "00:00", ["purpose"] = "First",
                ["ordinal"] = 0, ["durationSeconds"] = 30,
            });
        var second = await McpTestClient.CallAndReadJsonAsync<BeatDto>(client, "create_beat",
            new()
            {
                ["episodeId"] = episodeId, ["timecode"] = "01:00", ["purpose"] = "Second",
                ["ordinal"] = 1, ["durationSeconds"] = 30,
            });

        var reordered = await McpTestClient.CallAndReadJsonAsync<bool>(client, "reorder_beats",
            new() { ["episodeId"] = episodeId, ["orderedBeatIds"] = new[] { second!.Id, first!.Id } });
        Assert.True(reordered);

        var listed = await McpTestClient.CallAndReadJsonAsync<List<BeatDto>>(client, "list_beats",
            new() { ["episodeId"] = episodeId });
        var ordered = listed!.OrderBy(b => b.Ordinal).ToList();
        Assert.Equal(second.Id, ordered[0].Id);
        Assert.Equal(first.Id, ordered[1].Id);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter BeatToolsTests`

Expected: FAIL.

- [ ] **Step 3: Implement `BeatTools`**

Create `src/ProductionBible.Api/Mcp/BeatTools.cs`:

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Mcp;

[McpServerToolType]
public class BeatTools(IBeatService beatService)
{
    [McpServerTool(Name = "list_beats"), Description("Lists every beat in an episode, in timeline order.")]
    public Task<IReadOnlyList<BeatDto>> ListBeats(
        [Description("The parent episode's id")] int episodeId) => beatService.GetByEpisodeAsync(episodeId);

    [McpServerTool(Name = "get_beat"), Description("Gets a single beat by id, or null if it doesn't exist.")]
    public Task<BeatDto?> GetBeat(
        [Description("The beat's id")] int beatId) => beatService.GetByIdAsync(beatId);

    [McpServerTool(Name = "create_beat"), Description("Creates a new beat (a timecode anchor with a narrative purpose) in an episode.")]
    public Task<BeatDto> CreateBeat(
        [Description("The parent episode's id")] int episodeId,
        [Description("Source timecode, e.g. '22:30'")] string timecode,
        [Description("The beat's narrative purpose, e.g. 'Cold open'")] string purpose,
        [Description("Display/timeline order within the episode, ascending")] int ordinal,
        [Description("How long this beat runs, in seconds")] int durationSeconds) =>
        beatService.CreateAsync(episodeId, new CreateBeatRequest(timecode, purpose, ordinal, durationSeconds));

    [McpServerTool(Name = "update_beat"), Description("Updates an existing beat's timecode, purpose, order, and duration.")]
    public Task<BeatDto?> UpdateBeat(
        [Description("The beat's id")] int beatId,
        [Description("Source timecode, e.g. '22:30'")] string timecode,
        [Description("The beat's narrative purpose")] string purpose,
        [Description("Display/timeline order within the episode, ascending")] int ordinal,
        [Description("How long this beat runs, in seconds")] int durationSeconds) =>
        beatService.UpdateAsync(beatId, new UpdateBeatRequest(timecode, purpose, ordinal, durationSeconds));

    [McpServerTool(Name = "delete_beat"), Description("Deletes a beat. Returns false if it didn't exist.")]
    public Task<bool> DeleteBeat(
        [Description("The beat's id")] int beatId) => beatService.DeleteAsync(beatId);

    [McpServerTool(Name = "reorder_beats"), Description("Reorders every beat in an episode to match the given id sequence.")]
    public Task<bool> ReorderBeats(
        [Description("The parent episode's id")] int episodeId,
        [Description("Every beat id in the episode, in the desired new order")] int[] orderedBeatIds) =>
        beatService.ReorderAsync(episodeId, orderedBeatIds);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter BeatToolsTests`

Expected: PASS, all 4 tests.

- [ ] **Step 5: Run the full backend suite**

Run: `dotnet test ProductionBible.sln`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/ProductionBible.Api/Mcp/BeatTools.cs tests/ProductionBible.Api.Tests/Mcp/BeatToolsTests.cs
git commit -m "Add MCP tools for Beat (list/get/create/update/delete/reorder)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Task 7: `AssetTools`

**Files:**
- Create: `src/ProductionBible.Api/Mcp/AssetTools.cs`
- Create: `tests/ProductionBible.Api.Tests/Mcp/AssetToolsTests.cs`

**Interfaces:**
- Consumes: `McpTestClient` (Task 1), `create_project`/`create_episode`/`create_asset_type`/`create_beat`/`create_phase` tools (Tasks 2-6), `IAssetService` (existing).
- Produces: `list_assets`, `get_asset`, `create_asset`, `update_asset`, `delete_asset`, `reorder_assets_within_phase`, `reorder_assets_within_beat`.

This is the largest and most change-prone entity in the app — `Asset`'s DTO shape has changed twice already this project (`PhaseId` added in ordering-infrastructure, `OrderInPhase` added in Track B item 5). The field list below was confirmed by reading `src/ProductionBible.Application/Dtos/AssetDtos.cs` directly at plan-writing time — re-read that file before starting this task anyway, in case it has changed since:

```csharp
public record CreateAssetRequest(
    int AssetTypeId, string Code, string Title, string? ScriptText, string Status,
    string? Notes, int? SequenceNumber, int? TargetLengthSeconds,
    Dictionary<string, string>? Attributes, int[]? BeatIds, int? PhaseId = null);

public record UpdateAssetRequest(
    int AssetTypeId, string Code, string Title, string? ScriptText, string Status,
    string? Notes, int? SequenceNumber, int? TargetLengthSeconds,
    Dictionary<string, string>? Attributes, int[]? BeatIds, int? PhaseId = null);
```

Note `Attributes`/`BeatIds` come *before* `PhaseId` (not after), both are nullable, and `PhaseId` has a default of `null` — positional record construction must match this exact order. If the real file differs from the above when you read it, use the file's actual shape for every remaining step in this task — this is the authoritative source, not this plan.

- [ ] **Step 1: Re-read `AssetDtos.cs` and confirm the request shapes are still as above**

Run: `cat src/ProductionBible.Application/Dtos/AssetDtos.cs` (or open it directly). Confirm before proceeding.

- [ ] **Step 2: Write the failing tests**

Create `tests/ProductionBible.Api.Tests/Mcp/AssetToolsTests.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests.Mcp;

public class AssetToolsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AssetToolsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<(int episodeId, int assetTypeId)> CreateFixturesAsync(
        ModelContextProtocol.Client.McpClient client)
    {
        var project = await McpTestClient.CallAndReadJsonAsync<ProjectDto>(client, "create_project",
            new() { ["name"] = "Asset test project", ["description"] = (string?)null });
        var episode = await McpTestClient.CallAndReadJsonAsync<EpisodeDto>(client, "create_episode",
            new() { ["projectId"] = project!.Id, ["name"] = "EP1", ["orderIndex"] = 0 });
        var assetType = await McpTestClient.CallAndReadJsonAsync<AssetTypeDto>(client, "create_asset_type",
            new() { ["name"] = "Shot" });
        return (episode!.Id, assetType!.Id);
    }

    private static Dictionary<string, object?> BaseCreateArgs(int episodeId, int assetTypeId, string code) => new()
    {
        ["episodeId"] = episodeId,
        ["assetTypeId"] = assetTypeId,
        ["code"] = code,
        ["title"] = "A test shot",
        ["scriptText"] = (string?)null,
        ["status"] = "Planned",
        ["notes"] = (string?)null,
        ["sequenceNumber"] = (int?)null,
        ["targetLengthSeconds"] = (int?)null,
        ["phaseId"] = (int?)null,
        ["attributes"] = new Dictionary<string, string>(),
        ["beatIds"] = Array.Empty<int>(),
    };

    [Fact]
    public async Task Create_then_get_then_list_round_trips_an_asset()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var (episodeId, assetTypeId) = await CreateFixturesAsync(client);

        var created = await McpTestClient.CallAndReadJsonAsync<AssetDto>(client, "create_asset",
            BaseCreateArgs(episodeId, assetTypeId, "A-01"));

        Assert.Equal("A-01", created!.Code);

        var fetched = await McpTestClient.CallAndReadJsonAsync<AssetDto>(client, "get_asset",
            new() { ["assetId"] = created.Id });
        Assert.Equal("A-01", fetched!.Code);

        var listed = await McpTestClient.CallAndReadJsonAsync<List<AssetDto>>(client, "list_assets",
            new() { ["episodeId"] = episodeId });
        Assert.Contains(listed!, a => a.Id == created.Id);
    }

    [Fact]
    public async Task Update_changes_the_assets_fields_without_dropping_any()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var (episodeId, assetTypeId) = await CreateFixturesAsync(client);
        var created = await McpTestClient.CallAndReadJsonAsync<AssetDto>(client, "create_asset",
            BaseCreateArgs(episodeId, assetTypeId, "A-01"));

        var updateArgs = BaseCreateArgs(episodeId, assetTypeId, "A-01");
        updateArgs.Remove("episodeId");
        updateArgs["assetId"] = created!.Id;
        updateArgs["status"] = "Shot";
        updateArgs["sequenceNumber"] = 5;

        var updated = await McpTestClient.CallAndReadJsonAsync<AssetDto>(client, "update_asset", updateArgs);

        Assert.Equal("Shot", updated!.Status);
        Assert.Equal(5, updated.SequenceNumber);
        // Guards against a repeat of this project's own historical bug where an
        // update silently dropped PhaseId — confirm every field round-trips, not
        // just the two this test intentionally changed.
        Assert.Equal("A-01", updated.Code);
        Assert.Equal("A test shot", updated.Title);
    }

    [Fact]
    public async Task Delete_removes_the_asset()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var (episodeId, assetTypeId) = await CreateFixturesAsync(client);
        var created = await McpTestClient.CallAndReadJsonAsync<AssetDto>(client, "create_asset",
            BaseCreateArgs(episodeId, assetTypeId, "To delete"));

        var deleted = await McpTestClient.CallAndReadJsonAsync<bool>(client, "delete_asset",
            new() { ["assetId"] = created!.Id });
        Assert.True(deleted);

        var fetched = await McpTestClient.CallAndReadJsonAsync<AssetDto?>(client, "get_asset",
            new() { ["assetId"] = created.Id });
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Reorder_within_phase_persists_the_new_order()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var (episodeId, assetTypeId) = await CreateFixturesAsync(client);
        var episode = await McpTestClient.CallAndReadJsonAsync<EpisodeDto>(client, "get_episode",
            new() { ["episodeId"] = episodeId });
        var phase = await McpTestClient.CallAndReadJsonAsync<PhaseDto>(client, "create_phase",
            new() { ["projectId"] = episode!.ProjectId, ["name"] = "Phase 1", ["orderIndex"] = 0 });

        var firstArgs = BaseCreateArgs(episodeId, assetTypeId, "A-01");
        firstArgs["phaseId"] = phase!.Id;
        var first = await McpTestClient.CallAndReadJsonAsync<AssetDto>(client, "create_asset", firstArgs);

        var secondArgs = BaseCreateArgs(episodeId, assetTypeId, "A-02");
        secondArgs["phaseId"] = phase.Id;
        var second = await McpTestClient.CallAndReadJsonAsync<AssetDto>(client, "create_asset", secondArgs);

        var reordered = await McpTestClient.CallAndReadJsonAsync<bool>(client, "reorder_assets_within_phase",
            new() { ["phaseId"] = phase.Id, ["orderedAssetIds"] = new[] { second!.Id, first!.Id } });
        Assert.True(reordered);
    }

    [Fact]
    public async Task Reorder_within_beat_persists_the_new_order()
    {
        await using var client = await McpTestClient.ConnectAsync(_factory);
        var (episodeId, assetTypeId) = await CreateFixturesAsync(client);
        var beat = await McpTestClient.CallAndReadJsonAsync<BeatDto>(client, "create_beat",
            new()
            {
                ["episodeId"] = episodeId, ["timecode"] = "00:00", ["purpose"] = "Cold open",
                ["ordinal"] = 0, ["durationSeconds"] = 30,
            });

        var firstArgs = BaseCreateArgs(episodeId, assetTypeId, "A-01");
        firstArgs["beatIds"] = new[] { beat!.Id };
        var first = await McpTestClient.CallAndReadJsonAsync<AssetDto>(client, "create_asset", firstArgs);

        var secondArgs = BaseCreateArgs(episodeId, assetTypeId, "A-02");
        secondArgs["beatIds"] = new[] { beat.Id };
        var second = await McpTestClient.CallAndReadJsonAsync<AssetDto>(client, "create_asset", secondArgs);

        var reordered = await McpTestClient.CallAndReadJsonAsync<bool>(client, "reorder_assets_within_beat",
            new() { ["beatId"] = beat.Id, ["orderedAssetIds"] = new[] { second!.Id, first!.Id } });
        Assert.True(reordered);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter AssetToolsTests`

Expected: FAIL.

- [ ] **Step 4: Implement `AssetTools`**

Create `src/ProductionBible.Api/Mcp/AssetTools.cs`, using the field list confirmed in Step 1 (shown here as of this plan's writing — adjust to match if the actual file differs):

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Mcp;

[McpServerToolType]
public class AssetTools(IAssetService assetService)
{
    [McpServerTool(Name = "list_assets"), Description("Lists every asset in an episode.")]
    public Task<IReadOnlyList<AssetDto>> ListAssets(
        [Description("The parent episode's id")] int episodeId) => assetService.GetByEpisodeAsync(episodeId);

    [McpServerTool(Name = "get_asset"), Description("Gets a single asset by id, or null if it doesn't exist.")]
    public Task<AssetDto?> GetAsset(
        [Description("The asset's id")] int assetId) => assetService.GetByIdAsync(assetId);

    [McpServerTool(Name = "create_asset"), Description("Creates a new asset (the atomic production unit) in an episode.")]
    public Task<AssetDto> CreateAsset(
        [Description("The parent episode's id")] int episodeId,
        [Description("The asset's type id (Shot, VoiceOver, Graphic, etc.)")] int assetTypeId,
        [Description("Short code, e.g. 'A-01' or 'F-01'")] string code,
        [Description("The asset's title")] string title,
        [Description("Script or voiceover text, if any")] string? scriptText,
        [Description("Production status, e.g. Planned/Scripted/Shot/Rendered/Complete")] string status,
        [Description("Free-text notes")] string? notes,
        [Description("Shoot/production order (the project's overall running order)")] int? sequenceNumber,
        [Description("Target length in seconds, if known")] int? targetLengthSeconds,
        [Description("The shoot-day phase this asset belongs to, if any")] int? phaseId,
        [Description("Arbitrary key/value attributes")] Dictionary<string, string> attributes,
        [Description("Storyboard beats this asset is linked to")] int[] beatIds) =>
        assetService.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, code, title, scriptText, status, notes,
            sequenceNumber, targetLengthSeconds, attributes, beatIds, phaseId));

    [McpServerTool(Name = "update_asset"), Description("Updates an existing asset. Every field is required except the nullable ones — pass the asset's current value for anything you don't intend to change.")]
    public Task<AssetDto?> UpdateAsset(
        [Description("The asset's id")] int assetId,
        [Description("The asset's type id")] int assetTypeId,
        [Description("Short code, e.g. 'A-01'")] string code,
        [Description("The asset's title")] string title,
        [Description("Script or voiceover text, if any")] string? scriptText,
        [Description("Production status")] string status,
        [Description("Free-text notes")] string? notes,
        [Description("Shoot/production order")] int? sequenceNumber,
        [Description("Target length in seconds, if known")] int? targetLengthSeconds,
        [Description("The shoot-day phase this asset belongs to, if any")] int? phaseId,
        [Description("Arbitrary key/value attributes")] Dictionary<string, string> attributes,
        [Description("Storyboard beats this asset is linked to")] int[] beatIds) =>
        assetService.UpdateAsync(assetId, new UpdateAssetRequest(
            assetTypeId, code, title, scriptText, status, notes,
            sequenceNumber, targetLengthSeconds, attributes, beatIds, phaseId));

    [McpServerTool(Name = "delete_asset"), Description("Deletes an asset. Returns false if it didn't exist.")]
    public Task<bool> DeleteAsset(
        [Description("The asset's id")] int assetId) => assetService.DeleteAsync(assetId);

    [McpServerTool(Name = "reorder_assets_within_phase"), Description("Reorders every asset within a phase to match the given id sequence.")]
    public Task<bool> ReorderAssetsWithinPhase(
        [Description("The phase's id")] int phaseId,
        [Description("Every asset id in the phase, in the desired new order")] int[] orderedAssetIds) =>
        assetService.ReorderWithinPhaseAsync(phaseId, orderedAssetIds);

    [McpServerTool(Name = "reorder_assets_within_beat"), Description("Reorders every asset linked to a beat to match the given id sequence.")]
    public Task<bool> ReorderAssetsWithinBeat(
        [Description("The beat's id")] int beatId,
        [Description("Every asset id linked to the beat, in the desired new order")] int[] orderedAssetIds) =>
        assetService.ReorderWithinBeatAsync(beatId, orderedAssetIds);
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/ProductionBible.Api.Tests --filter AssetToolsTests`

Expected: PASS, all 5 tests.

- [ ] **Step 6: Run the full backend suite**

Run: `dotnet test ProductionBible.sln`

Expected: PASS — this is the final task, so this run should show every MCP tool test (Tasks 1-7) plus every pre-existing backend test green together.

- [ ] **Step 7: Verify the tool count matches the spec**

Add (or extend `McpServerWiringTests.cs` with) a final assertion that `client.ListToolsAsync()` returns exactly 33 tools once every task is done — this is the concrete check that nothing was missed or duplicated across the six entity tasks:

```csharp
[Fact]
public async Task All_33_tools_are_registered()
{
    await using var client = await McpTestClient.ConnectAsync(_factory);

    var tools = await client.ListToolsAsync();

    Assert.Equal(33, tools.Count);
}
```

Run: `dotnet test tests/ProductionBible.Api.Tests --filter McpServerWiringTests`

Expected: PASS. If the count doesn't match 33, find the discrepancy (a missed method, a duplicated tool name, or a miscount in this plan) before committing — don't adjust the assertion to match an unexplained actual count.

- [ ] **Step 8: Commit**

```bash
git add src/ProductionBible.Api/Mcp/AssetTools.cs tests/ProductionBible.Api.Tests/Mcp/AssetToolsTests.cs \
        tests/ProductionBible.Api.Tests/Mcp/McpServerWiringTests.cs
git commit -m "Add MCP tools for Asset (list/get/create/update/delete/reorder x2)

Completes full CRUD parity across all 6 entities (33 tools total).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01676u59fyVZuf5Z2XBbmw5J"
```

---

## Self-Review Notes

- **Spec coverage:** every tool in the spec's inventory (33 total, 6 entity classes) maps to exactly one task. Hosting/transport/route (Task 1), testing approach (reusing `TestWebApplicationFactory`, Task 1's `McpTestClient` helper), naming convention (`snake_case`, verb-first) and parameter-flattening rule are all followed consistently across every task's tool implementations.
- **Placeholder scan:** Task 1's `McpTestClient` transport-construction line and result-deserialization logic are the one deliberately-flagged unknown in this plan — explicitly bounded (a single method's internals, with a clear compiling/passing-test success criterion) rather than an open-ended "figure it out." Every other task's code is complete and exact. Task 7's DTO field list is explicitly sourced from live inspection of `AssetDtos.cs` rather than asserted as fact, per this project's own repeated DTO-drift history.
- **Type consistency:** `McpTestClient.CallAndReadJsonAsync<T>`'s signature (established in Task 1) is used identically by every later task. Tool names, parameter names, and parameter order match between each task's test file and its implementation file (e.g. `create_project`'s `name`/`description` args in both `ProjectToolsTests.cs` and `ProjectTools.cs`).
- **Scope:** 7 tasks, all additive (no refactor of existing REST API code). No backend service, DTO, or migration changes — this plan only adds a new MCP-facing layer over the unchanged service layer.
