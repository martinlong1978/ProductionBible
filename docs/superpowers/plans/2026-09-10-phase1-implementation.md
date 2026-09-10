# ProductionBible Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Phase 1 slice of ProductionBible — a data model unifying storyboard/beat-sheet and shoot-day production-plan content into one SQLite-backed store, a REST API over it, an Angular UI with Bible (+ Timeline display mode) and Production Plan views, and a one-time import script that turns the real HalfNut ELS `storyboard.html` + `production_plan.md` into seed data.

**Architecture:** One ASP.NET Core process (`ProductionBible.Api`) hosts a REST API backed by EF Core/SQLite, with business logic in a service layer (`ProductionBible.Application`) that a later MCP server (Phase 2) will call directly. The Angular SPA (`web/`) is built and served as static files from the same Kestrel process. A separate console tool (`ProductionBible.Importer`) does the one-time seed import, reusing `ProductionBible.Application`'s entities and DbContext.

**Tech Stack:** .NET 10 (LTS, SDK 10.0.401 confirmed current as of 2026-09-08) + ASP.NET Core Web API + EF Core 10 + SQLite + xUnit + Microsoft.AspNetCore.Mvc.Testing + HtmlAgilityPack (storyboard.html parsing). Angular 22 (latest CLI confirmed installed: 22.1.7) + standalone components + Jasmine/Karma (Angular CLI default).

**Spec:** `docs/superpowers/specs/2026-09-10-phase1-core-design.md`

## Global Constraints

- **Domain-neutral schema and code.** No machining/lathe vocabulary in entity names, DTO names, field labels, or import-mapping *code* — only imported *data values* are HalfNut-ELS-specific. The generalized staging-info field is `SceneSetup`, not "machine configuration".
- **AssetType is an extensible reference table, not a hard-coded enum.** New types must not require a code change.
- **Type-specific Asset fields live in `AssetAttribute` (key/value), never as new columns.** Only the fields common to every asset type get real columns on `Asset`.
- **One process at runtime.** The Angular build output is served as static files by the same Kestrel process that serves the API. Binds to `0.0.0.0`. No authentication.
- **Service layer, not controller logic.** All business logic lives in `ProductionBible.Application` services; controllers only translate HTTP ↔ service calls. This boundary is required so Phase 2's MCP tools can call the same services directly.
- **No live sync.** The import script runs once against a fresh database. It is not idempotent against a populated database and is not required to be.
- **Structured fields only.** No markdown/rich-text document editor in Phase 1.
- **Printable export, auth, MCP, Shot Complete timestamps, media matching, and teleprompter push are all out of scope for Phase 1.** Don't build hooks for them beyond the nullable `CompletedAtUtc` column already called for by the spec.
- **Every commit in this plan ends with:**
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
  ```

---

## File Structure

```
ProductionBible/
  ProductionBible.sln
  .gitignore
  README.md
  src/
    ProductionBible.Application/
      ProductionBible.Application.csproj
      Entities/
        Project.cs
        Episode.cs
        Beat.cs
        AssetType.cs
        Asset.cs
        AssetAttribute.cs
        AssetBeat.cs
      Dtos/
        ProjectDtos.cs
        EpisodeDtos.cs
        AssetTypeDtos.cs
        BeatDtos.cs
        AssetDtos.cs
      Data/
        ProductionBibleDbContext.cs
        ProductionBibleDbContextFactory.cs
        Migrations/            (EF Core generated)
      Services/
        IProjectService.cs / ProjectService.cs
        IEpisodeService.cs / EpisodeService.cs
        IAssetTypeService.cs / AssetTypeService.cs
        IBeatService.cs / BeatService.cs
        IAssetService.cs / AssetService.cs
    ProductionBible.Api/
      ProductionBible.Api.csproj
      Program.cs
      appsettings.json
      Controllers/
        ProjectsController.cs
        EpisodesController.cs
        AssetTypesController.cs
        BeatsController.cs
        AssetsController.cs
      wwwroot/
        .gitkeep
    ProductionBible.Importer/
      ProductionBible.Importer.csproj
      Program.cs
      Models/
        ParsedShotRow.cs
        ParsedAnimationRow.cs
        ParsedShotPage.cs
      StoryboardHtmlParser.cs
      ProductionPlanMarkdownParser.cs
      ImportMapper.cs
  tests/
    ProductionBible.Application.Tests/
      ProductionBible.Application.Tests.csproj
      ProjectServiceTests.cs
      EpisodeServiceTests.cs
      AssetTypeServiceTests.cs
      BeatServiceTests.cs
      AssetServiceTests.cs
    ProductionBible.Api.Tests/
      ProductionBible.Api.Tests.csproj
      ProjectsApiTests.cs
      AssetsApiTests.cs
    ProductionBible.Importer.Tests/
      ProductionBible.Importer.Tests.csproj
      Fixtures/
        storyboard.html        (verbatim copy of the real file)
        production_plan.md     (verbatim copy of the real file)
      StoryboardHtmlParserTests.cs
      ProductionPlanMarkdownParserTests.cs
      ImportMapperTests.cs
  web/
    (Angular workspace "web", generated by `ng new`)
    src/app/
      core/
        api-client.service.ts
        api-client.service.spec.ts
        models.ts
      bible/
        bible.component.ts
        bible.component.html
        bible.component.spec.ts
        timeline-view.component.ts
        timeline-view.component.html
        timeline-view.component.spec.ts
      production-plan/
        production-plan.component.ts
        production-plan.component.html
        production-plan.component.spec.ts
      app.routes.ts
      app.config.ts
```

Rationale for the `Application` / `Api` / `Importer` split (rather than a deeper Clean-Architecture layering): the spec's only hard requirement is "service layer beneath controllers, callable directly by Phase 2's MCP tools." A single `Application` class library holding entities, DbContext, and services satisfies that with the least ceremony — YAGNI against an unused abstraction layer this app doesn't need yet.

---

### Task 1: Solution scaffold

**Files:**
- Create: `ProductionBible.sln`
- Create: `.gitignore`
- Create: `src/ProductionBible.Application/ProductionBible.Application.csproj`
- Create: `src/ProductionBible.Api/ProductionBible.Api.csproj`
- Create: `src/ProductionBible.Importer/ProductionBible.Importer.csproj`
- Create: `tests/ProductionBible.Application.Tests/ProductionBible.Application.Tests.csproj`
- Create: `tests/ProductionBible.Api.Tests/ProductionBible.Api.Tests.csproj`
- Create: `tests/ProductionBible.Importer.Tests/ProductionBible.Importer.Tests.csproj`

**Interfaces:**
- Produces: a buildable, empty solution with all 6 projects wired together, referenced by every later task.

- [ ] **Step 1: Install the .NET 10 SDK**

The workstation has the .NET runtime but no SDK (`dotnet --version` reports "No .NET SDKs were found"). Install it:

```bash
winget install Microsoft.DotNet.SDK.10
```

If `winget` is unavailable, download the installer from `https://dotnet.microsoft.com/en-us/download/dotnet/10.0` instead. After install, open a new shell and confirm:

```bash
dotnet --version
```

Expected: a `10.x.x` version string (e.g. `10.0.401`).

- [ ] **Step 2: Create the solution and project skeletons**

From `D:\Data\source\ProductionBible`:

```bash
dotnet new sln -n ProductionBible

dotnet new classlib -n ProductionBible.Application -o src/ProductionBible.Application -f net10.0
dotnet new webapi -n ProductionBible.Api -o src/ProductionBible.Api -f net10.0 --use-controllers
dotnet new console -n ProductionBible.Importer -o src/ProductionBible.Importer -f net10.0

dotnet new xunit -n ProductionBible.Application.Tests -o tests/ProductionBible.Application.Tests -f net10.0
dotnet new xunit -n ProductionBible.Api.Tests -o tests/ProductionBible.Api.Tests -f net10.0
dotnet new xunit -n ProductionBible.Importer.Tests -o tests/ProductionBible.Importer.Tests -f net10.0

dotnet sln add src/ProductionBible.Application/ProductionBible.Application.csproj
dotnet sln add src/ProductionBible.Api/ProductionBible.Api.csproj
dotnet sln add src/ProductionBible.Importer/ProductionBible.Importer.csproj
dotnet sln add tests/ProductionBible.Application.Tests/ProductionBible.Application.Tests.csproj
dotnet sln add tests/ProductionBible.Api.Tests/ProductionBible.Api.Tests.csproj
dotnet sln add tests/ProductionBible.Importer.Tests/ProductionBible.Importer.Tests.csproj
```

Delete the template placeholder files the templates generate that we don't want:

```bash
rm src/ProductionBible.Api/WeatherForecast.cs
rm src/ProductionBible.Api/Controllers/WeatherForecastController.cs
rm src/ProductionBible.Application/Class1.cs
```

- [ ] **Step 3: Wire project references**

```bash
dotnet add src/ProductionBible.Api/ProductionBible.Api.csproj reference src/ProductionBible.Application/ProductionBible.Application.csproj
dotnet add src/ProductionBible.Importer/ProductionBible.Importer.csproj reference src/ProductionBible.Application/ProductionBible.Application.csproj

dotnet add tests/ProductionBible.Application.Tests/ProductionBible.Application.Tests.csproj reference src/ProductionBible.Application/ProductionBible.Application.csproj
dotnet add tests/ProductionBible.Api.Tests/ProductionBible.Api.Tests.csproj reference src/ProductionBible.Api/ProductionBible.Api.csproj
dotnet add tests/ProductionBible.Importer.Tests/ProductionBible.Importer.Tests.csproj reference src/ProductionBible.Importer/ProductionBible.Importer.csproj
```

- [ ] **Step 4: Add NuGet packages needed across the plan**

```bash
dotnet add src/ProductionBible.Application/ProductionBible.Application.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/ProductionBible.Application/ProductionBible.Application.csproj package Microsoft.EntityFrameworkCore.Design

dotnet add src/ProductionBible.Importer/ProductionBible.Importer.csproj package HtmlAgilityPack

dotnet add tests/ProductionBible.Application.Tests/ProductionBible.Application.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/ProductionBible.Api.Tests/ProductionBible.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
```

- [ ] **Step 5: Add `.gitignore`**

```
bin/
obj/
*.db
*.db-shm
*.db-wal
web/node_modules/
web/dist/
.vs/
```

- [ ] **Step 6: Write a smoke test to prove the solution builds and tests run**

Create `tests/ProductionBible.Application.Tests/SmokeTests.cs`:

```csharp
namespace ProductionBible.Application.Tests;

public class SmokeTests
{
    [Fact]
    public void Solution_builds_and_tests_run()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 7: Run the full test suite to verify it builds and passes**

```bash
dotnet test ProductionBible.sln
```

Expected: all 3 test projects build, the one smoke test in `ProductionBible.Application.Tests` passes, the other two test projects report 0 tests (their template placeholder tests were not removed yet — that's fine, they'll gain real content in later tasks; if the xunit template left a `UnitTest1.cs` with a failing/no-op test in the other two projects, delete those placeholder files now so `dotnet test` is clean).

```bash
rm -f tests/ProductionBible.Api.Tests/UnitTest1.cs
rm -f tests/ProductionBible.Importer.Tests/UnitTest1.cs
dotnet test ProductionBible.sln
```

Expected: PASS, 1 test total.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Scaffold solution: Application/Api/Importer projects + test projects

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 2: Domain entities + EF Core DbContext + SQLite migration

**Files:**
- Create: `src/ProductionBible.Application/Entities/Project.cs`
- Create: `src/ProductionBible.Application/Entities/Episode.cs`
- Create: `src/ProductionBible.Application/Entities/Beat.cs`
- Create: `src/ProductionBible.Application/Entities/AssetType.cs`
- Create: `src/ProductionBible.Application/Entities/Asset.cs`
- Create: `src/ProductionBible.Application/Entities/AssetAttribute.cs`
- Create: `src/ProductionBible.Application/Entities/AssetBeat.cs`
- Create: `src/ProductionBible.Application/Data/ProductionBibleDbContext.cs`
- Create: `src/ProductionBible.Application/Data/ProductionBibleDbContextFactory.cs`
- Test: `tests/ProductionBible.Application.Tests/DbContextRoundTripTests.cs`

**Interfaces:**
- Produces: `ProductionBibleDbContext` with `DbSet<Project> Projects`, `DbSet<Episode> Episodes`, `DbSet<Beat> Beats`, `DbSet<AssetType> AssetTypes`, `DbSet<Asset> Assets`, `DbSet<AssetAttribute> AssetAttributes`, `DbSet<AssetBeat> AssetBeats`. All entity classes below, with exactly these property names/types — every later task's services and the importer depend on this exact shape.

- [ ] **Step 1: Write the entity classes**

`src/ProductionBible.Application/Entities/Project.cs`:

```csharp
namespace ProductionBible.Application.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public List<Episode> Episodes { get; set; } = new();
}
```

`src/ProductionBible.Application/Entities/Episode.cs`:

```csharp
namespace ProductionBible.Application.Entities;

public class Episode
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = "";
    public int OrderIndex { get; set; }

    public List<Beat> Beats { get; set; } = new();
    public List<Asset> Assets { get; set; } = new();
}
```

`src/ProductionBible.Application/Entities/Beat.cs`:

```csharp
namespace ProductionBible.Application.Entities;

public class Beat
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    public string Timecode { get; set; } = "";
    public string Purpose { get; set; } = "";

    public List<AssetBeat> AssetBeats { get; set; } = new();
}
```

`src/ProductionBible.Application/Entities/AssetType.cs`:

```csharp
namespace ProductionBible.Application.Entities;

public class AssetType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

`src/ProductionBible.Application/Entities/Asset.cs`:

```csharp
namespace ProductionBible.Application.Entities;

public class Asset
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public Episode? Episode { get; set; }
    public int AssetTypeId { get; set; }
    public AssetType? AssetType { get; set; }

    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string? ScriptText { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Notes { get; set; }
    public int? SequenceNumber { get; set; }
    public int? TargetLengthSeconds { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public List<AssetAttribute> Attributes { get; set; } = new();
    public List<AssetBeat> AssetBeats { get; set; } = new();
}
```

`src/ProductionBible.Application/Entities/AssetAttribute.cs`:

```csharp
namespace ProductionBible.Application.Entities;

public class AssetAttribute
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }

    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
```

`src/ProductionBible.Application/Entities/AssetBeat.cs`:

```csharp
namespace ProductionBible.Application.Entities;

public class AssetBeat
{
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int BeatId { get; set; }
    public Beat? Beat { get; set; }
}
```

- [ ] **Step 2: Write the DbContext**

`src/ProductionBible.Application/Data/ProductionBibleDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Data;

public class ProductionBibleDbContext : DbContext
{
    public ProductionBibleDbContext(DbContextOptions<ProductionBibleDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Beat> Beats => Set<Beat>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetAttribute> AssetAttributes => Set<AssetAttribute>();
    public DbSet<AssetBeat> AssetBeats => Set<AssetBeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssetBeat>(e =>
        {
            e.HasKey(ab => new { ab.AssetId, ab.BeatId });
            e.HasOne(ab => ab.Asset).WithMany(a => a.AssetBeats).HasForeignKey(ab => ab.AssetId);
            e.HasOne(ab => ab.Beat).WithMany(b => b.AssetBeats).HasForeignKey(ab => ab.BeatId);
        });

        modelBuilder.Entity<AssetType>().HasIndex(t => t.Name).IsUnique();
        modelBuilder.Entity<Asset>().HasIndex(a => a.Code);

        modelBuilder.Entity<Episode>()
            .HasOne(e => e.Project).WithMany(p => p.Episodes).HasForeignKey(e => e.ProjectId);
        modelBuilder.Entity<Beat>()
            .HasOne(b => b.Episode).WithMany(e => e.Beats).HasForeignKey(b => b.EpisodeId);
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.Episode).WithMany(e => e.Assets).HasForeignKey(a => a.EpisodeId);
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.AssetType).WithMany().HasForeignKey(a => a.AssetTypeId);
        modelBuilder.Entity<AssetAttribute>()
            .HasOne(at => at.Asset).WithMany(a => a.Attributes).HasForeignKey(at => at.AssetId);
    }
}
```

`src/ProductionBible.Application/Data/ProductionBibleDbContextFactory.cs` (lets `dotnet ef` design-time tooling create a context without full DI):

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductionBible.Application.Data;

public class ProductionBibleDbContextFactory : IDesignTimeDbContextFactory<ProductionBibleDbContext>
{
    public ProductionBibleDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseSqlite("Data Source=design_time.db")
            .Options;
        return new ProductionBibleDbContext(options);
    }
}
```

- [ ] **Step 3: Generate the initial migration**

```bash
cd src/ProductionBible.Application
dotnet ef migrations add InitialCreate
cd ../..
```

If `dotnet ef` is not found, install the tool once: `dotnet tool install --global dotnet-ef`.

Expected: a `Migrations/` folder appears under `src/ProductionBible.Application/` with `<timestamp>_InitialCreate.cs` and `ProductionBibleDbContextModelSnapshot.cs`.

- [ ] **Step 4: Write the round-trip integration test**

`tests/ProductionBible.Application.Tests/DbContextRoundTripTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Tests;

public class DbContextRoundTripTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"pb_test_{Guid.NewGuid():N}.db");

    private ProductionBibleDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        var context = new ProductionBibleDbContext(options);
        context.Database.Migrate();
        return context;
    }

    [Fact]
    public void Can_save_and_reload_a_full_project_graph()
    {
        using (var context = CreateContext())
        {
            var assetType = new AssetType { Name = "Shot" };
            var project = new Project { Name = "HalfNut ELS" };
            var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
            var beat = new Beat { Episode = episode, Timecode = "00:00", Purpose = "Cold open" };
            var asset = new Asset
            {
                Episode = episode,
                AssetType = assetType,
                Code = "A-01",
                Title = "Tool entering the work",
                Status = "Planned",
            };
            asset.Attributes.Add(new AssetAttribute { Asset = asset, Key = "SceneSetup", Value = "Steel bar, ~25mm" });
            asset.AssetBeats.Add(new AssetBeat { Asset = asset, Beat = beat });

            context.Projects.Add(project);
            context.SaveChanges();
        }

        using (var context = CreateContext())
        {
            var reloaded = context.Projects
                .Include(p => p.Episodes).ThenInclude(e => e.Assets).ThenInclude(a => a.Attributes)
                .Include(p => p.Episodes).ThenInclude(e => e.Assets).ThenInclude(a => a.AssetBeats)
                .Include(p => p.Episodes).ThenInclude(e => e.Beats)
                .Single();

            Assert.Equal("HalfNut ELS", reloaded.Name);
            var episode = Assert.Single(reloaded.Episodes);
            var asset = Assert.Single(episode.Assets);
            Assert.Equal("A-01", asset.Code);
            var attribute = Assert.Single(asset.Attributes);
            Assert.Equal("SceneSetup", attribute.Key);
            Assert.Equal("Steel bar, ~25mm", attribute.Value);
            Assert.Single(asset.AssetBeats);
            Assert.Single(episode.Beats);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

```bash
dotnet test tests/ProductionBible.Application.Tests
```

Expected: PASS (this is a round-trip test against a real generated migration, not a TDD red-then-green cycle, because there's no meaningful failing state to write first for a schema-definition task — the migration either applies and round-trips or it doesn't).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Add domain entities, DbContext, and initial SQLite migration

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 3: Project resource — DTOs, service, controller

This task establishes the CRUD pattern every later resource (Episode, AssetType, Beat, Asset) repeats: a `Dtos` file, an `I<X>Service`/`<X>Service` pair operating on DTOs (never leaking EF entities past the service layer), and a thin controller.

**Files:**
- Create: `src/ProductionBible.Application/Dtos/ProjectDtos.cs`
- Create: `src/ProductionBible.Application/Services/IProjectService.cs`
- Create: `src/ProductionBible.Application/Services/ProjectService.cs`
- Create: `src/ProductionBible.Api/Controllers/ProjectsController.cs`
- Test: `tests/ProductionBible.Application.Tests/ProjectServiceTests.cs`

**Interfaces:**
- Consumes: `ProductionBibleDbContext` (Task 2).
- Produces: `IProjectService` with `GetAllAsync()`, `GetByIdAsync(int id)`, `CreateAsync(CreateProjectRequest)`, `UpdateAsync(int id, UpdateProjectRequest)`, `DeleteAsync(int id)`. This exact method-name pattern (`GetAllAsync`/`GetByIdAsync`/`CreateAsync`/`UpdateAsync`/`DeleteAsync`) is reused by every later service interface.

- [ ] **Step 1: Write the DTOs**

`src/ProductionBible.Application/Dtos/ProjectDtos.cs`:

```csharp
namespace ProductionBible.Application.Dtos;

public record ProjectDto(int Id, string Name, string? Description);

public record CreateProjectRequest(string Name, string? Description);

public record UpdateProjectRequest(string Name, string? Description);
```

- [ ] **Step 2: Write the failing service test**

`tests/ProductionBible.Application.Tests/ProjectServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class ProjectServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_round_trips_the_project()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);

        var created = await service.CreateAsync(new CreateProjectRequest("HalfNut ELS", "The lathe series"));

        Assert.True(created.Id > 0);
        Assert.Equal("HalfNut ELS", created.Name);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("HalfNut ELS", fetched!.Name);
        Assert.Equal("The lathe series", fetched.Description);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_returns_every_project()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);
        await service.CreateAsync(new CreateProjectRequest("Project One", null));
        await service.CreateAsync(new CreateProjectRequest("Project Two", null));

        var all = await service.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task UpdateAsync_changes_name_and_description()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);
        var created = await service.CreateAsync(new CreateProjectRequest("Old Name", null));

        var updated = await service.UpdateAsync(created.Id, new UpdateProjectRequest("New Name", "New description"));

        Assert.NotNull(updated);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal("New description", updated.Description);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_project_and_returns_true()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);
        var created = await service.CreateAsync(new CreateProjectRequest("To Delete", null));

        var deleted = await service.DeleteAsync(created.Id);
        var fetched = await service.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteAsync_returns_false_for_unknown_id()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);

        var deleted = await service.DeleteAsync(999);

        Assert.False(deleted);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter ProjectServiceTests
```

Expected: FAIL to compile — `IProjectService`/`ProjectService` don't exist yet.

- [ ] **Step 4: Write the service interface and implementation**

`src/ProductionBible.Application/Services/IProjectService.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> GetAllAsync();
    Task<ProjectDto?> GetByIdAsync(int id);
    Task<ProjectDto> CreateAsync(CreateProjectRequest request);
    Task<ProjectDto?> UpdateAsync(int id, UpdateProjectRequest request);
    Task<bool> DeleteAsync(int id);
}
```

`src/ProductionBible.Application/Services/ProjectService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class ProjectService : IProjectService
{
    private readonly ProductionBibleDbContext _db;

    public ProjectService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync()
    {
        return await _db.Projects
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description))
            .ToListAsync();
    }

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        return project is null ? null : new ProjectDto(project.Id, project.Name, project.Description);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request)
    {
        var project = new Project { Name = request.Name, Description = request.Description };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return new ProjectDto(project.Id, project.Name, project.Description);
    }

    public async Task<ProjectDto?> UpdateAsync(int id, UpdateProjectRequest request)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return null;

        project.Name = request.Name;
        project.Description = request.Description;
        await _db.SaveChangesAsync();
        return new ProjectDto(project.Id, project.Name, project.Description);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return false;

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return true;
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter ProjectServiceTests
```

Expected: PASS, 6 tests.

- [ ] **Step 6: Write the controller**

`src/ProductionBible.Api/Controllers/ProjectsController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectsController(IProjectService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(int id)
    {
        var project = await _service.GetByIdAsync(id);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request)
    {
        var created = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectDto>> Update(int id, UpdateProjectRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

Note: this controller is not runnable end-to-end yet — `Program.cs` does not register `IProjectService` or the DbContext in DI until Task 8. That is expected; Task 8's smoke test is what proves the wiring.

- [ ] **Step 7: Build to verify the controller compiles**

```bash
dotnet build src/ProductionBible.Api
```

Expected: builds (controller compiles against the service interface; DI registration comes later).

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add Project CRUD: service, controller, and service tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---
