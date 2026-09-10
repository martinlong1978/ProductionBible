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

### Task 4: Episode resource — DTOs, service, controller

**Files:**
- Create: `src/ProductionBible.Application/Dtos/EpisodeDtos.cs`
- Create: `src/ProductionBible.Application/Services/IEpisodeService.cs`
- Create: `src/ProductionBible.Application/Services/EpisodeService.cs`
- Create: `src/ProductionBible.Api/Controllers/EpisodesController.cs`
- Test: `tests/ProductionBible.Application.Tests/EpisodeServiceTests.cs`

**Interfaces:**
- Consumes: `ProductionBibleDbContext` (Task 2), `Project` entity (Task 2).
- Produces: `IEpisodeService` with `GetByProjectAsync(int projectId)`, `GetByIdAsync(int id)`, `CreateAsync(int projectId, CreateEpisodeRequest)`, `UpdateAsync(int id, UpdateEpisodeRequest)`, `DeleteAsync(int id)`.

- [ ] **Step 1: Write the DTOs**

`src/ProductionBible.Application/Dtos/EpisodeDtos.cs`:

```csharp
namespace ProductionBible.Application.Dtos;

public record EpisodeDto(int Id, int ProjectId, string Name, int OrderIndex);

public record CreateEpisodeRequest(string Name, int OrderIndex);

public record UpdateEpisodeRequest(string Name, int OrderIndex);
```

- [ ] **Step 2: Write the failing service test**

`tests/ProductionBible.Application.Tests/EpisodeServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class EpisodeServiceTests
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
    public async Task CreateAsync_then_GetByIdAsync_round_trips_the_episode()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new EpisodeService(context);

        var created = await service.CreateAsync(projectId, new CreateEpisodeRequest("EP1", 1));

        Assert.True(created.Id > 0);
        Assert.Equal(projectId, created.ProjectId);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("EP1", fetched!.Name);
        Assert.Equal(1, fetched.OrderIndex);
    }

    [Fact]
    public async Task GetByProjectAsync_returns_only_that_projects_episodes_in_order()
    {
        await using var context = CreateInMemoryContext();
        var projectAId = await SeedProjectAsync(context);
        var projectBId = await SeedProjectAsync(context);
        var service = new EpisodeService(context);
        await service.CreateAsync(projectAId, new CreateEpisodeRequest("EP2", 2));
        await service.CreateAsync(projectAId, new CreateEpisodeRequest("EP1", 1));
        await service.CreateAsync(projectBId, new CreateEpisodeRequest("Other Project EP1", 1));

        var episodes = await service.GetByProjectAsync(projectAId);

        Assert.Equal(2, episodes.Count);
        Assert.Equal("EP1", episodes[0].Name);
        Assert.Equal("EP2", episodes[1].Name);
    }

    [Fact]
    public async Task UpdateAsync_changes_name_and_order()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new EpisodeService(context);
        var created = await service.CreateAsync(projectId, new CreateEpisodeRequest("Draft Name", 1));

        var updated = await service.UpdateAsync(created.Id, new UpdateEpisodeRequest("EP1", 1));

        Assert.NotNull(updated);
        Assert.Equal("EP1", updated!.Name);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_episode()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new EpisodeService(context);
        var created = await service.CreateAsync(projectId, new CreateEpisodeRequest("EP1", 1));

        var deleted = await service.DeleteAsync(created.Id);
        var fetched = await service.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(fetched);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter EpisodeServiceTests
```

Expected: FAIL to compile — `IEpisodeService`/`EpisodeService` don't exist yet.

- [ ] **Step 4: Write the service interface and implementation**

`src/ProductionBible.Application/Services/IEpisodeService.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IEpisodeService
{
    Task<IReadOnlyList<EpisodeDto>> GetByProjectAsync(int projectId);
    Task<EpisodeDto?> GetByIdAsync(int id);
    Task<EpisodeDto> CreateAsync(int projectId, CreateEpisodeRequest request);
    Task<EpisodeDto?> UpdateAsync(int id, UpdateEpisodeRequest request);
    Task<bool> DeleteAsync(int id);
}
```

`src/ProductionBible.Application/Services/EpisodeService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class EpisodeService : IEpisodeService
{
    private readonly ProductionBibleDbContext _db;

    public EpisodeService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    private static EpisodeDto ToDto(Episode e) => new(e.Id, e.ProjectId, e.Name, e.OrderIndex);

    public async Task<IReadOnlyList<EpisodeDto>> GetByProjectAsync(int projectId)
    {
        return await _db.Episodes
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.OrderIndex)
            .Select(e => new EpisodeDto(e.Id, e.ProjectId, e.Name, e.OrderIndex))
            .ToListAsync();
    }

    public async Task<EpisodeDto?> GetByIdAsync(int id)
    {
        var episode = await _db.Episodes.FindAsync(id);
        return episode is null ? null : ToDto(episode);
    }

    public async Task<EpisodeDto> CreateAsync(int projectId, CreateEpisodeRequest request)
    {
        var episode = new Episode { ProjectId = projectId, Name = request.Name, OrderIndex = request.OrderIndex };
        _db.Episodes.Add(episode);
        await _db.SaveChangesAsync();
        return ToDto(episode);
    }

    public async Task<EpisodeDto?> UpdateAsync(int id, UpdateEpisodeRequest request)
    {
        var episode = await _db.Episodes.FindAsync(id);
        if (episode is null) return null;

        episode.Name = request.Name;
        episode.OrderIndex = request.OrderIndex;
        await _db.SaveChangesAsync();
        return ToDto(episode);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var episode = await _db.Episodes.FindAsync(id);
        if (episode is null) return false;

        _db.Episodes.Remove(episode);
        await _db.SaveChangesAsync();
        return true;
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter EpisodeServiceTests
```

Expected: PASS, 4 tests.

- [ ] **Step 6: Write the controller**

`src/ProductionBible.Api/Controllers/EpisodesController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
public class EpisodesController : ControllerBase
{
    private readonly IEpisodeService _service;

    public EpisodesController(IEpisodeService service)
    {
        _service = service;
    }

    [HttpGet("api/projects/{projectId:int}/episodes")]
    public async Task<ActionResult<IReadOnlyList<EpisodeDto>>> GetByProject(int projectId)
        => Ok(await _service.GetByProjectAsync(projectId));

    [HttpGet("api/episodes/{id:int}")]
    public async Task<ActionResult<EpisodeDto>> GetById(int id)
    {
        var episode = await _service.GetByIdAsync(id);
        return episode is null ? NotFound() : Ok(episode);
    }

    [HttpPost("api/projects/{projectId:int}/episodes")]
    public async Task<ActionResult<EpisodeDto>> Create(int projectId, CreateEpisodeRequest request)
    {
        var created = await _service.CreateAsync(projectId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("api/episodes/{id:int}")]
    public async Task<ActionResult<EpisodeDto>> Update(int id, UpdateEpisodeRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("api/episodes/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

- [ ] **Step 7: Build to verify the controller compiles**

```bash
dotnet build src/ProductionBible.Api
```

Expected: builds.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add Episode CRUD: service, controller, and service tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 5: AssetType resource — DTOs, service, controller

AssetType is the extensible type registry (the Global Constraint that new asset types must not require a code change flows through this resource — an agent or user creates a new `AssetType` row via this same API instead of a schema change).

**Files:**
- Create: `src/ProductionBible.Application/Dtos/AssetTypeDtos.cs`
- Create: `src/ProductionBible.Application/Services/IAssetTypeService.cs`
- Create: `src/ProductionBible.Application/Services/AssetTypeService.cs`
- Create: `src/ProductionBible.Api/Controllers/AssetTypesController.cs`
- Test: `tests/ProductionBible.Application.Tests/AssetTypeServiceTests.cs`

**Interfaces:**
- Consumes: `ProductionBibleDbContext` (Task 2).
- Produces: `IAssetTypeService` with `GetAllAsync()`, `GetByIdAsync(int id)`, `CreateAsync(CreateAssetTypeRequest)`, `DeleteAsync(int id)`. No `UpdateAsync` — a type's identity is its name; renaming is delete-and-recreate, and Task 7 (Asset) will look up `AssetTypeId` by name during creation.

- [ ] **Step 1: Write the DTOs**

`src/ProductionBible.Application/Dtos/AssetTypeDtos.cs`:

```csharp
namespace ProductionBible.Application.Dtos;

public record AssetTypeDto(int Id, string Name);

public record CreateAssetTypeRequest(string Name);
```

- [ ] **Step 2: Write the failing service test**

`tests/ProductionBible.Application.Tests/AssetTypeServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class AssetTypeServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_then_GetAllAsync_returns_the_new_type()
    {
        await using var context = CreateInMemoryContext();
        var service = new AssetTypeService(context);

        var created = await service.CreateAsync(new CreateAssetTypeRequest("Shot"));
        var all = await service.GetAllAsync();

        Assert.True(created.Id > 0);
        Assert.Contains(all, t => t.Name == "Shot");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        await using var context = CreateInMemoryContext();
        var service = new AssetTypeService(context);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_name()
    {
        await using var context = CreateInMemoryContext();
        var service = new AssetTypeService(context);
        await service.CreateAsync(new CreateAssetTypeRequest("Shot"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(new CreateAssetTypeRequest("Shot")));
    }

    [Fact]
    public async Task DeleteAsync_removes_the_type()
    {
        await using var context = CreateInMemoryContext();
        var service = new AssetTypeService(context);
        var created = await service.CreateAsync(new CreateAssetTypeRequest("Animation"));

        var deleted = await service.DeleteAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(created.Id));
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter AssetTypeServiceTests
```

Expected: FAIL to compile — `IAssetTypeService`/`AssetTypeService` don't exist yet.

- [ ] **Step 4: Write the service interface and implementation**

`src/ProductionBible.Application/Services/IAssetTypeService.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IAssetTypeService
{
    Task<IReadOnlyList<AssetTypeDto>> GetAllAsync();
    Task<AssetTypeDto?> GetByIdAsync(int id);
    Task<AssetTypeDto> CreateAsync(CreateAssetTypeRequest request);
    Task<bool> DeleteAsync(int id);
}
```

`src/ProductionBible.Application/Services/AssetTypeService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class AssetTypeService : IAssetTypeService
{
    private readonly ProductionBibleDbContext _db;

    public AssetTypeService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AssetTypeDto>> GetAllAsync()
    {
        return await _db.AssetTypes
            .Select(t => new AssetTypeDto(t.Id, t.Name))
            .ToListAsync();
    }

    public async Task<AssetTypeDto?> GetByIdAsync(int id)
    {
        var type = await _db.AssetTypes.FindAsync(id);
        return type is null ? null : new AssetTypeDto(type.Id, type.Name);
    }

    public async Task<AssetTypeDto> CreateAsync(CreateAssetTypeRequest request)
    {
        var exists = await _db.AssetTypes.AnyAsync(t => t.Name == request.Name);
        if (exists)
        {
            throw new InvalidOperationException($"An asset type named '{request.Name}' already exists.");
        }

        var type = new AssetType { Name = request.Name };
        _db.AssetTypes.Add(type);
        await _db.SaveChangesAsync();
        return new AssetTypeDto(type.Id, type.Name);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var type = await _db.AssetTypes.FindAsync(id);
        if (type is null) return false;

        _db.AssetTypes.Remove(type);
        await _db.SaveChangesAsync();
        return true;
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter AssetTypeServiceTests
```

Expected: PASS, 4 tests.

- [ ] **Step 6: Write the controller**

`src/ProductionBible.Api/Controllers/AssetTypesController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
[Route("api/asset-types")]
public class AssetTypesController : ControllerBase
{
    private readonly IAssetTypeService _service;

    public AssetTypesController(IAssetTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetTypeDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetTypeDto>> GetById(int id)
    {
        var type = await _service.GetByIdAsync(id);
        return type is null ? NotFound() : Ok(type);
    }

    [HttpPost]
    public async Task<ActionResult<AssetTypeDto>> Create(CreateAssetTypeRequest request)
    {
        try
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

- [ ] **Step 7: Build to verify the controller compiles**

```bash
dotnet build src/ProductionBible.Api
```

Expected: builds.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add AssetType CRUD: service, controller, and service tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 6: Beat resource — DTOs, service, controller

**Files:**
- Create: `src/ProductionBible.Application/Dtos/BeatDtos.cs`
- Create: `src/ProductionBible.Application/Services/IBeatService.cs`
- Create: `src/ProductionBible.Application/Services/BeatService.cs`
- Create: `src/ProductionBible.Api/Controllers/BeatsController.cs`
- Test: `tests/ProductionBible.Application.Tests/BeatServiceTests.cs`

**Interfaces:**
- Consumes: `ProductionBibleDbContext`, `Episode` entity (Task 2).
- Produces: `IBeatService` with `GetByEpisodeAsync(int episodeId)`, `GetByIdAsync(int id)`, `CreateAsync(int episodeId, CreateBeatRequest)`, `UpdateAsync(int id, UpdateBeatRequest)`, `DeleteAsync(int id)`. `BeatDto` includes `AssetIds` (populated via `AssetBeat`, Task 2) so Task 7's Asset↔Beat linking has something to read; Beat itself never writes `AssetBeat` rows — only `AssetService` (Task 7) does, since the link is created from the Asset side (`CreateAssetRequest.BeatIds`).

- [ ] **Step 1: Write the DTOs**

`src/ProductionBible.Application/Dtos/BeatDtos.cs`:

```csharp
namespace ProductionBible.Application.Dtos;

public record BeatDto(int Id, int EpisodeId, string Timecode, string Purpose, int[] AssetIds);

public record CreateBeatRequest(string Timecode, string Purpose);

public record UpdateBeatRequest(string Timecode, string Purpose);
```

- [ ] **Step 2: Write the failing service test**

`tests/ProductionBible.Application.Tests/BeatServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class BeatServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    private static async Task<int> SeedEpisodeAsync(ProductionBibleDbContext context)
    {
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        context.Episodes.Add(episode);
        await context.SaveChangesAsync();
        return episode.Id;
    }

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_round_trips_the_beat()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var service = new BeatService(context);

        var created = await service.CreateAsync(episodeId, new CreateBeatRequest("00:00", "Cold open"));

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("00:00", fetched!.Timecode);
        Assert.Equal("Cold open", fetched.Purpose);
        Assert.Empty(fetched.AssetIds);
    }

    [Fact]
    public async Task GetByEpisodeAsync_returns_beats_for_that_episode_only()
    {
        await using var context = CreateInMemoryContext();
        var episodeAId = await SeedEpisodeAsync(context);
        var episodeBId = await SeedEpisodeAsync(context);
        var service = new BeatService(context);
        await service.CreateAsync(episodeAId, new CreateBeatRequest("00:00", "Cold open"));
        await service.CreateAsync(episodeBId, new CreateBeatRequest("00:00", "Different episode"));

        var beats = await service.GetByEpisodeAsync(episodeAId);

        Assert.Single(beats);
        Assert.Equal("Cold open", beats[0].Purpose);
    }

    [Fact]
    public async Task GetByIdAsync_includes_linked_asset_ids()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var assetType = new AssetType { Name = "Shot" };
        context.AssetTypes.Add(assetType);
        var beat = new Beat { EpisodeId = episodeId, Timecode = "00:00", Purpose = "Cold open" };
        context.Beats.Add(beat);
        var asset = new Asset { EpisodeId = episodeId, AssetType = assetType, Code = "A-01", Title = "Entry" };
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        context.AssetBeats.Add(new AssetBeat { AssetId = asset.Id, BeatId = beat.Id });
        await context.SaveChangesAsync();

        var service = new BeatService(context);
        var fetched = await service.GetByIdAsync(beat.Id);

        Assert.NotNull(fetched);
        Assert.Equal(new[] { asset.Id }, fetched!.AssetIds);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_beat()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var service = new BeatService(context);
        var created = await service.CreateAsync(episodeId, new CreateBeatRequest("00:00", "Cold open"));

        var deleted = await service.DeleteAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(created.Id));
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter BeatServiceTests
```

Expected: FAIL to compile — `IBeatService`/`BeatService` don't exist yet.

- [ ] **Step 4: Write the service interface and implementation**

`src/ProductionBible.Application/Services/IBeatService.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IBeatService
{
    Task<IReadOnlyList<BeatDto>> GetByEpisodeAsync(int episodeId);
    Task<BeatDto?> GetByIdAsync(int id);
    Task<BeatDto> CreateAsync(int episodeId, CreateBeatRequest request);
    Task<BeatDto?> UpdateAsync(int id, UpdateBeatRequest request);
    Task<bool> DeleteAsync(int id);
}
```

`src/ProductionBible.Application/Services/BeatService.cs`:

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
        return await _db.Beats
            .Where(b => b.EpisodeId == episodeId)
            .Select(b => new BeatDto(
                b.Id, b.EpisodeId, b.Timecode, b.Purpose,
                b.AssetBeats.Select(ab => ab.AssetId).ToArray()))
            .ToListAsync();
    }

    public async Task<BeatDto?> GetByIdAsync(int id)
    {
        return await _db.Beats
            .Where(b => b.Id == id)
            .Select(b => new BeatDto(
                b.Id, b.EpisodeId, b.Timecode, b.Purpose,
                b.AssetBeats.Select(ab => ab.AssetId).ToArray()))
            .SingleOrDefaultAsync();
    }

    public async Task<BeatDto> CreateAsync(int episodeId, CreateBeatRequest request)
    {
        var beat = new Beat { EpisodeId = episodeId, Timecode = request.Timecode, Purpose = request.Purpose };
        _db.Beats.Add(beat);
        await _db.SaveChangesAsync();
        return new BeatDto(beat.Id, beat.EpisodeId, beat.Timecode, beat.Purpose, Array.Empty<int>());
    }

    public async Task<BeatDto?> UpdateAsync(int id, UpdateBeatRequest request)
    {
        var beat = await _db.Beats.FindAsync(id);
        if (beat is null) return null;

        beat.Timecode = request.Timecode;
        beat.Purpose = request.Purpose;
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

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter BeatServiceTests
```

Expected: PASS, 4 tests.

- [ ] **Step 6: Write the controller**

`src/ProductionBible.Api/Controllers/BeatsController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
public class BeatsController : ControllerBase
{
    private readonly IBeatService _service;

    public BeatsController(IBeatService service)
    {
        _service = service;
    }

    [HttpGet("api/episodes/{episodeId:int}/beats")]
    public async Task<ActionResult<IReadOnlyList<BeatDto>>> GetByEpisode(int episodeId)
        => Ok(await _service.GetByEpisodeAsync(episodeId));

    [HttpGet("api/beats/{id:int}")]
    public async Task<ActionResult<BeatDto>> GetById(int id)
    {
        var beat = await _service.GetByIdAsync(id);
        return beat is null ? NotFound() : Ok(beat);
    }

    [HttpPost("api/episodes/{episodeId:int}/beats")]
    public async Task<ActionResult<BeatDto>> Create(int episodeId, CreateBeatRequest request)
    {
        var created = await _service.CreateAsync(episodeId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("api/beats/{id:int}")]
    public async Task<ActionResult<BeatDto>> Update(int id, UpdateBeatRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("api/beats/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

- [ ] **Step 7: Build to verify the controller compiles**

```bash
dotnet build src/ProductionBible.Api
```

Expected: builds.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add Beat CRUD: service, controller, and service tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 7: Asset resource — DTOs, service, controller (attributes + beat links)

This is the most complex resource: every create/update carries a full `Attributes` dictionary and `BeatIds` array, and the service must replace (not merge) both on every write, matching PUT-style full-replace semantics.

**Files:**
- Create: `src/ProductionBible.Application/Dtos/AssetDtos.cs`
- Create: `src/ProductionBible.Application/Services/IAssetService.cs`
- Create: `src/ProductionBible.Application/Services/AssetService.cs`
- Create: `src/ProductionBible.Api/Controllers/AssetsController.cs`
- Test: `tests/ProductionBible.Application.Tests/AssetServiceTests.cs`

**Interfaces:**
- Consumes: `ProductionBibleDbContext`, `Asset`/`AssetAttribute`/`AssetBeat`/`AssetType` entities (Task 2).
- Produces: `IAssetService` with `GetByEpisodeAsync(int episodeId)`, `GetByIdAsync(int id)`, `CreateAsync(int episodeId, CreateAssetRequest)`, `UpdateAsync(int id, UpdateAssetRequest)`, `DeleteAsync(int id)`. `AssetDto.Attributes` is `Dictionary<string,string>`; `AssetDto.BeatIds` is `int[]`. This is the exact shape Task 13-15 (Importer) will construct when writing seed data, and the exact shape Phase 2's MCP tools will read/write.

- [ ] **Step 1: Write the DTOs**

`src/ProductionBible.Application/Dtos/AssetDtos.cs`:

```csharp
namespace ProductionBible.Application.Dtos;

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
    DateTime? CompletedAtUtc,
    Dictionary<string, string> Attributes,
    int[] BeatIds);

public record CreateAssetRequest(
    int AssetTypeId,
    string Code,
    string Title,
    string? ScriptText,
    string Status,
    string? Notes,
    int? SequenceNumber,
    int? TargetLengthSeconds,
    Dictionary<string, string>? Attributes,
    int[]? BeatIds);

public record UpdateAssetRequest(
    int AssetTypeId,
    string Code,
    string Title,
    string? ScriptText,
    string Status,
    string? Notes,
    int? SequenceNumber,
    int? TargetLengthSeconds,
    Dictionary<string, string>? Attributes,
    int[]? BeatIds);
```

- [ ] **Step 2: Write the failing service test**

`tests/ProductionBible.Application.Tests/AssetServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class AssetServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    private static async Task<(int episodeId, int assetTypeId, int beatId)> SeedAsync(ProductionBibleDbContext context)
    {
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var assetType = new AssetType { Name = "Shot" };
        var beat = new Beat { Episode = episode, Timecode = "00:00", Purpose = "Cold open" };
        context.Episodes.Add(episode);
        context.AssetTypes.Add(assetType);
        context.Beats.Add(beat);
        await context.SaveChangesAsync();
        return (episode.Id, assetType.Id, beat.Id);
    }

    [Fact]
    public async Task CreateAsync_stores_attributes_and_beat_links()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);

        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            AssetTypeId: assetTypeId,
            Code: "A-01",
            Title: "Tool entering the work",
            ScriptText: null,
            Status: "Planned",
            Notes: null,
            SequenceNumber: 1,
            TargetLengthSeconds: null,
            Attributes: new Dictionary<string, string> { ["SceneSetup"] = "Steel bar, ~25mm" },
            BeatIds: new[] { beatId }));

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("A-01", fetched!.Code);
        Assert.Equal("Shot", fetched.AssetTypeName);
        Assert.Equal("Steel bar, ~25mm", fetched.Attributes["SceneSetup"]);
        Assert.Equal(new[] { beatId }, fetched.BeatIds);
    }

    [Fact]
    public async Task GetByEpisodeAsync_returns_assets_for_that_episode_only()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, _) = await SeedAsync(context);
        var otherEpisode = new Episode { ProjectId = (await context.Episodes.FindAsync(episodeId))!.ProjectId, Name = "EP2", OrderIndex = 2 };
        context.Episodes.Add(otherEpisode);
        await context.SaveChangesAsync();
        var service = new AssetService(context);
        await service.CreateAsync(episodeId, MinimalRequest(assetTypeId, "A-01"));
        await service.CreateAsync(otherEpisode.Id, MinimalRequest(assetTypeId, "B-01"));

        var assets = await service.GetByEpisodeAsync(episodeId);

        Assert.Single(assets);
        Assert.Equal("A-01", assets[0].Code);
    }

    [Fact]
    public async Task UpdateAsync_replaces_attributes_and_beat_links_rather_than_merging()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);
        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "Original title", null, "Planned", null, 1, null,
            new Dictionary<string, string> { ["SceneSetup"] = "Original setup" },
            new[] { beatId }));

        var updated = await service.UpdateAsync(created.Id, new UpdateAssetRequest(
            assetTypeId, "A-01", "Updated title", null, "Shot", null, 1, null,
            new Dictionary<string, string> { ["AngleAndCamera"] = "Macro on the tool" },
            Array.Empty<int>()));

        Assert.NotNull(updated);
        Assert.Equal("Updated title", updated!.Title);
        Assert.Equal("Shot", updated.Status);
        Assert.False(updated.Attributes.ContainsKey("SceneSetup"));
        Assert.Equal("Macro on the tool", updated.Attributes["AngleAndCamera"]);
        Assert.Empty(updated.BeatIds);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_asset_and_its_attributes()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);
        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "Title", null, "Planned", null, null, null,
            new Dictionary<string, string> { ["SceneSetup"] = "Setup" },
            new[] { beatId }));

        var deleted = await service.DeleteAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(created.Id));
        Assert.Empty(context.AssetAttributes.Where(a => a.AssetId == created.Id));
    }

    private static CreateAssetRequest MinimalRequest(int assetTypeId, string code) => new(
        assetTypeId, code, code, null, "Planned", null, null, null, null, null);
}
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter AssetServiceTests
```

Expected: FAIL to compile — `IAssetService`/`AssetService` don't exist yet.

- [ ] **Step 4: Write the service interface and implementation**

`src/ProductionBible.Application/Services/IAssetService.cs`:

```csharp
using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IAssetService
{
    Task<IReadOnlyList<AssetDto>> GetByEpisodeAsync(int episodeId);
    Task<AssetDto?> GetByIdAsync(int id);
    Task<AssetDto> CreateAsync(int episodeId, CreateAssetRequest request);
    Task<AssetDto?> UpdateAsync(int id, UpdateAssetRequest request);
    Task<bool> DeleteAsync(int id);
}
```

`src/ProductionBible.Application/Services/AssetService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class AssetService : IAssetService
{
    private readonly ProductionBibleDbContext _db;

    public AssetService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AssetDto>> GetByEpisodeAsync(int episodeId)
    {
        var assets = await _db.Assets
            .Include(a => a.AssetType)
            .Include(a => a.Attributes)
            .Include(a => a.AssetBeats)
            .Where(a => a.EpisodeId == episodeId)
            .ToListAsync();
        return assets.Select(ToDto).ToList();
    }

    public async Task<AssetDto?> GetByIdAsync(int id)
    {
        var asset = await _db.Assets
            .Include(a => a.AssetType)
            .Include(a => a.Attributes)
            .Include(a => a.AssetBeats)
            .SingleOrDefaultAsync(a => a.Id == id);
        return asset is null ? null : ToDto(asset);
    }

    public async Task<AssetDto> CreateAsync(int episodeId, CreateAssetRequest request)
    {
        var asset = new Asset
        {
            EpisodeId = episodeId,
            AssetTypeId = request.AssetTypeId,
            Code = request.Code,
            Title = request.Title,
            ScriptText = request.ScriptText,
            Status = request.Status,
            Notes = request.Notes,
            SequenceNumber = request.SequenceNumber,
            TargetLengthSeconds = request.TargetLengthSeconds,
        };
        ApplyAttributes(asset, request.Attributes);
        ApplyBeatLinks(asset, request.BeatIds);

        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(asset.Id))!;
    }

    public async Task<AssetDto?> UpdateAsync(int id, UpdateAssetRequest request)
    {
        var asset = await _db.Assets
            .Include(a => a.Attributes)
            .Include(a => a.AssetBeats)
            .SingleOrDefaultAsync(a => a.Id == id);
        if (asset is null) return null;

        asset.AssetTypeId = request.AssetTypeId;
        asset.Code = request.Code;
        asset.Title = request.Title;
        asset.ScriptText = request.ScriptText;
        asset.Status = request.Status;
        asset.Notes = request.Notes;
        asset.SequenceNumber = request.SequenceNumber;
        asset.TargetLengthSeconds = request.TargetLengthSeconds;

        _db.AssetAttributes.RemoveRange(asset.Attributes);
        asset.Attributes.Clear();
        ApplyAttributes(asset, request.Attributes);

        _db.AssetBeats.RemoveRange(asset.AssetBeats);
        asset.AssetBeats.Clear();
        ApplyBeatLinks(asset, request.BeatIds);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var asset = await _db.Assets
            .Include(a => a.Attributes)
            .Include(a => a.AssetBeats)
            .SingleOrDefaultAsync(a => a.Id == id);
        if (asset is null) return false;

        _db.AssetAttributes.RemoveRange(asset.Attributes);
        _db.AssetBeats.RemoveRange(asset.AssetBeats);
        _db.Assets.Remove(asset);
        await _db.SaveChangesAsync();
        return true;
    }

    private static void ApplyAttributes(Asset asset, Dictionary<string, string>? attributes)
    {
        if (attributes is null) return;
        foreach (var (key, value) in attributes)
        {
            asset.Attributes.Add(new AssetAttribute { Asset = asset, Key = key, Value = value });
        }
    }

    private static void ApplyBeatLinks(Asset asset, int[]? beatIds)
    {
        if (beatIds is null) return;
        foreach (var beatId in beatIds)
        {
            asset.AssetBeats.Add(new AssetBeat { Asset = asset, BeatId = beatId });
        }
    }

    private static AssetDto ToDto(Asset asset) => new(
        asset.Id,
        asset.EpisodeId,
        asset.AssetTypeId,
        asset.AssetType?.Name ?? "",
        asset.Code,
        asset.Title,
        asset.ScriptText,
        asset.Status,
        asset.Notes,
        asset.SequenceNumber,
        asset.TargetLengthSeconds,
        asset.CompletedAtUtc,
        asset.Attributes.ToDictionary(a => a.Key, a => a.Value),
        asset.AssetBeats.Select(ab => ab.BeatId).ToArray());
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet test tests/ProductionBible.Application.Tests --filter AssetServiceTests
```

Expected: PASS, 4 tests.

- [ ] **Step 6: Write the controller**

`src/ProductionBible.Api/Controllers/AssetsController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _service;

    public AssetsController(IAssetService service)
    {
        _service = service;
    }

    [HttpGet("api/episodes/{episodeId:int}/assets")]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetByEpisode(int episodeId)
        => Ok(await _service.GetByEpisodeAsync(episodeId));

    [HttpGet("api/assets/{id:int}")]
    public async Task<ActionResult<AssetDto>> GetById(int id)
    {
        var asset = await _service.GetByIdAsync(id);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpPost("api/episodes/{episodeId:int}/assets")]
    public async Task<ActionResult<AssetDto>> Create(int episodeId, CreateAssetRequest request)
    {
        var created = await _service.CreateAsync(episodeId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("api/assets/{id:int}")]
    public async Task<ActionResult<AssetDto>> Update(int id, UpdateAssetRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("api/assets/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

- [ ] **Step 7: Build to verify the controller compiles**

```bash
dotnet build src/ProductionBible.Api
```

Expected: builds.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add Asset CRUD: service, controller, and service tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 8: Program.cs wiring — DI, migrations-on-startup, static files, LAN binding

**Files:**
- Modify: `src/ProductionBible.Api/Program.cs`
- Modify: `src/ProductionBible.Api/appsettings.json`
- Create: `src/ProductionBible.Api/wwwroot/.gitkeep`
- Test: `tests/ProductionBible.Api.Tests/ProjectsApiTests.cs`

**Interfaces:**
- Consumes: every `I<X>Service`/`<X>Service` pair from Tasks 3-7, `ProductionBibleDbContext` (Task 2).
- Produces: a running `WebApplication` reachable on `0.0.0.0:5280`, with all 5 controllers live behind real DI, migrations applied automatically on startup, and static files served from `wwwroot` (empty until Task 16 builds the Angular app into it). Exposes `public partial class Program` so `WebApplicationFactory<Program>` can host it in tests.

- [ ] **Step 1: Write the failing integration test**

`tests/ProductionBible.Api.Tests/ProjectsApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests;

public class ProjectsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProjectsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_then_get_round_trips_a_project_through_the_real_http_pipeline()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/projects", new CreateProjectRequest("HalfNut ELS", "The lathe series"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

        Assert.NotNull(created);
        var getResponse = await client.GetAsync($"/api/projects/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.Equal("HalfNut ELS", fetched!.Name);
    }

    [Fact]
    public async Task Get_unknown_project_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/projects/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test tests/ProductionBible.Api.Tests
```

Expected: FAIL — either a compile error (`Program` isn't `partial`/accessible yet, since the default minimal-API template's `Program.cs` top-level statements produce an implicit `Program` class that WebApplicationFactory can normally still find, but only once the DbContext/services are registered; without DI wiring the app will fail to start with a `DbContext not registered` / service-resolution exception at first request).

- [ ] **Step 3: Write `appsettings.json`**

`src/ProductionBible.Api/appsettings.json` (extend the template's generated file — keep its existing `Logging`/`AllowedHosts` keys, add `ConnectionStrings`):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Data Source={0}"
  }
}
```

`{0}` is a placeholder `Program.cs` substitutes with the resolved `App_Data` path — SQLite connection strings need an absolute or working-directory-relative path, and the working directory differs between `dotnet run`, a published exe, and the test host, so the path is resolved in code rather than hard-coded here.

- [ ] **Step 4: Write `Program.cs`**

`src/ProductionBible.Api/Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5280");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "productionbible.db");
var connectionStringTemplate = builder.Configuration.GetConnectionString("Default") ?? "Data Source={0}";
var connectionString = string.Format(connectionStringTemplate, dbPath);

builder.Services.AddDbContext<ProductionBibleDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();
builder.Services.AddScoped<IBeatService, BeatService>();
builder.Services.AddScoped<IAssetService, AssetService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductionBibleDbContext>();
    db.Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program
{
}
```

Note: `WebApplicationFactory<Program>` in tests overrides the connection string is not needed here — because `dbPath` is computed under `ContentRootPath/App_Data`, and the test host's content root is the `ProductionBible.Api` build output directory, each test run reuses/creates `App_Data/productionbible.db` there. That's acceptable for Phase 1 (tests run against a real, migrated SQLite file, which also exercises the actual migration path) but means test runs are not isolated from each other's data. Accept this for now — Task 8's tests only assert on data they themselves created and look up by the ID they got back, so shared state doesn't make them flaky. If this becomes a problem later, override `IClassFixture` with a `WebApplicationFactory` that swaps in a per-test-run SQLite file via `builder.ConfigureServices`.

- [ ] **Step 5: Run the test to verify it passes**

```bash
dotnet test tests/ProductionBible.Api.Tests
```

Expected: PASS, 2 tests.

- [ ] **Step 6: Run the full solution test suite**

```bash
dotnet test ProductionBible.sln
```

Expected: PASS — every test from Tasks 1-8 (smoke test, DbContext round-trip, 5 services' CRUD tests, 2 API integration tests).

- [ ] **Step 7: Create the empty `wwwroot` placeholder**

```bash
mkdir -p src/ProductionBible.Api/wwwroot
touch src/ProductionBible.Api/wwwroot/.gitkeep
```

(`.gitignore` from Task 1 does not exclude `wwwroot`, only `bin/`/`obj/` — confirm the folder is tracked by checking `git status` shows the new `.gitkeep`.)

- [ ] **Step 8: Manually verify the app runs and binds to 0.0.0.0**

```bash
dotnet run --project src/ProductionBible.Api &
sleep 3
curl -s http://localhost:5280/api/projects
kill %1
```

Expected: `curl` returns `[]` (empty JSON array — no projects yet) with no connection error, confirming the app started, applied migrations, and is listening.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Wire DI, migrations-on-startup, and static file serving in Program.cs

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 9: Angular workspace scaffold + typed API client service

**Files:**
- Create: `web/` (generated Angular workspace)
- Create: `web/proxy.conf.json`
- Create: `web/src/app/core/models.ts`
- Create: `web/src/app/core/api-client.service.ts`
- Test: `web/src/app/core/api-client.service.spec.ts`

**Interfaces:**
- Consumes: the REST API from Tasks 3-8 (`/api/projects`, `/api/projects/{id}/episodes`, `/api/episodes/{id}/beats`, `/api/episodes/{id}/assets`, `/api/assets/{id}`, `/api/asset-types`).
- Produces: `ApiClientService` with `getProjects()`, `getEpisodes(projectId)`, `getBeats(episodeId)`, `getAssets(episodeId)`, `getAssetTypes()`, `updateAsset(id, request)` — all returning RxJS `Observable`s of the TypeScript interfaces in `models.ts`. Tasks 10-12 (Bible/Timeline/Production Plan views) consume this service exclusively; they never call `HttpClient` directly.

- [ ] **Step 1: Scaffold the Angular workspace**

From `D:\Data\source\ProductionBible`:

```bash
npx -y @angular/cli@latest new web --directory=web --routing --style=css --skip-git
```

If the CLI interactively prompts for a unit-test runner, choose **Jasmine** (the classic default — this plan's test examples use Jasmine/Karma syntax). If it prompts for AI tooling or SSR, decline both (this is a client-rendered SPA with no AI-tooling need).

- [ ] **Step 2: Add a dev-server proxy so `ng serve` reaches the API**

`web/proxy.conf.json`:

```json
{
  "/api": {
    "target": "http://localhost:5280",
    "secure": false
  }
}
```

Document the dev workflow by adding a `start` script that uses it — edit `web/package.json`'s `scripts` section:

```json
"start": "ng serve --proxy-config proxy.conf.json"
```

- [ ] **Step 3: Write the TypeScript models**

`web/src/app/core/models.ts`:

```typescript
export interface ProjectDto {
  id: number;
  name: string;
  description: string | null;
}

export interface EpisodeDto {
  id: number;
  projectId: number;
  name: string;
  orderIndex: number;
}

export interface AssetTypeDto {
  id: number;
  name: string;
}

export interface BeatDto {
  id: number;
  episodeId: number;
  timecode: string;
  purpose: string;
  assetIds: number[];
}

export interface AssetDto {
  id: number;
  episodeId: number;
  assetTypeId: number;
  assetTypeName: string;
  code: string;
  title: string;
  scriptText: string | null;
  status: string;
  notes: string | null;
  sequenceNumber: number | null;
  targetLengthSeconds: number | null;
  completedAtUtc: string | null;
  attributes: Record<string, string>;
  beatIds: number[];
}

export interface UpdateAssetRequest {
  assetTypeId: number;
  code: string;
  title: string;
  scriptText: string | null;
  status: string;
  notes: string | null;
  sequenceNumber: number | null;
  targetLengthSeconds: number | null;
  attributes: Record<string, string> | null;
  beatIds: number[] | null;
}
```

- [ ] **Step 4: Write the failing service test**

`web/src/app/core/api-client.service.spec.ts`:

```typescript
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ApiClientService } from './api-client.service';
import { ProjectDto, AssetDto } from './models';

describe('ApiClientService', () => {
  let service: ApiClientService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApiClientService],
    });
    service = TestBed.inject(ApiClientService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('fetches projects from /api/projects', () => {
    const expected: ProjectDto[] = [{ id: 1, name: 'HalfNut ELS', description: null }];

    service.getProjects().subscribe((projects) => {
      expect(projects).toEqual(expected);
    });

    const req = httpMock.expectOne('/api/projects');
    expect(req.request.method).toBe('GET');
    req.flush(expected);
  });

  it('fetches assets for an episode from /api/episodes/{id}/assets', () => {
    const expected: AssetDto[] = [];

    service.getAssets(5).subscribe((assets) => {
      expect(assets).toEqual(expected);
    });

    const req = httpMock.expectOne('/api/episodes/5/assets');
    expect(req.request.method).toBe('GET');
    req.flush(expected);
  });

  it('sends a PUT to /api/assets/{id} for updateAsset', () => {
    service.updateAsset(7, {
      assetTypeId: 1, code: 'A-01', title: 'Title', scriptText: null,
      status: 'Shot', notes: 'Went well', sequenceNumber: 1, targetLengthSeconds: null,
      attributes: null, beatIds: null,
    }).subscribe();

    const req = httpMock.expectOne('/api/assets/7');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });
});
```

- [ ] **Step 5: Run the tests to verify they fail**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: FAIL to compile — `ApiClientService` does not exist yet.

- [ ] **Step 6: Write the service**

`web/src/app/core/api-client.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AssetDto, AssetTypeDto, BeatDto, EpisodeDto, ProjectDto, UpdateAssetRequest } from './models';

@Injectable({ providedIn: 'root' })
export class ApiClientService {
  constructor(private readonly http: HttpClient) {}

  getProjects(): Observable<ProjectDto[]> {
    return this.http.get<ProjectDto[]>('/api/projects');
  }

  getEpisodes(projectId: number): Observable<EpisodeDto[]> {
    return this.http.get<EpisodeDto[]>(`/api/projects/${projectId}/episodes`);
  }

  getBeats(episodeId: number): Observable<BeatDto[]> {
    return this.http.get<BeatDto[]>(`/api/episodes/${episodeId}/beats`);
  }

  getAssets(episodeId: number): Observable<AssetDto[]> {
    return this.http.get<AssetDto[]>(`/api/episodes/${episodeId}/assets`);
  }

  getAssetTypes(): Observable<AssetTypeDto[]> {
    return this.http.get<AssetTypeDto[]>('/api/asset-types');
  }

  updateAsset(id: number, request: UpdateAssetRequest): Observable<AssetDto> {
    return this.http.put<AssetDto>(`/api/assets/${id}`, request);
  }
}
```

- [ ] **Step 7: Register `HttpClient` in the app config**

Edit `web/src/app/app.config.ts` to add `provideHttpClient()`:

```typescript
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [provideRouter(routes), provideHttpClient()],
};
```

- [ ] **Step 8: Run the tests to verify they pass**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: PASS, 3 tests.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Scaffold Angular workspace and add typed API client service

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 10: Bible view component (episode/timecode order, grouped by beat)

**Files:**
- Create: `web/src/app/bible/bible.component.ts`
- Create: `web/src/app/bible/bible.component.html`
- Test: `web/src/app/bible/bible.component.spec.ts`

**Interfaces:**
- Consumes: `ApiClientService.getProjects()`, `.getEpisodes()`, `.getBeats()`, `.getAssets()` (Task 9).
- Produces: `BibleComponent` with public methods `selectEpisode(episodeId: number)`, `assetsForBeat(beat: BeatDto): AssetDto[]`, `toggleTimeline(): void`, and a `unassignedAssets` getter. Task 11 (Timeline toggle) renders inside this component when `viewMode === 'timeline'`, reading the same `beats`/`assets` fields this task defines.

- [ ] **Step 1: Write the failing component test**

`web/src/app/bible/bible.component.spec.ts`:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { BibleComponent } from './bible.component';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto, BeatDto, EpisodeDto, ProjectDto } from '../core/models';

describe('BibleComponent', () => {
  let fixture: ComponentFixture<BibleComponent>;
  let component: BibleComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const beat: BeatDto = { id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open', assetIds: [1000] };
  const linkedAsset: AssetDto = {
    id: 1000, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01',
    title: 'Tool entering the work', scriptText: null, status: 'Planned', notes: null,
    sequenceNumber: 1, targetLengthSeconds: null, completedAtUtc: null, attributes: {}, beatIds: [100],
  };
  const unlinkedAsset: AssetDto = { ...linkedAsset, id: 1001, code: 'A-02', beatIds: [] };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getProjects', 'getEpisodes', 'getBeats', 'getAssets']);
    apiSpy.getProjects.and.returnValue(of([project]));
    apiSpy.getEpisodes.and.returnValue(of([episode]));
    apiSpy.getBeats.and.returnValue(of([beat]));
    apiSpy.getAssets.and.returnValue(of([linkedAsset, unlinkedAsset]));

    await TestBed.configureTestingModule({
      imports: [BibleComponent],
      providers: [{ provide: ApiClientService, useValue: apiSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(BibleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the first episode of the first project on init', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(component.selectedEpisodeId).toBe(10);
    expect(component.beats).toEqual([beat]);
  });

  it('groups assets under the beat that links to them', () => {
    expect(component.assetsForBeat(beat)).toEqual([linkedAsset]);
  });

  it('lists assets with no beat link as unassigned', () => {
    expect(component.unassignedAssets).toEqual([unlinkedAsset]);
  });

  it('toggles view mode between list and timeline', () => {
    expect(component.viewMode).toBe('list');
    component.toggleTimeline();
    expect(component.viewMode).toBe('timeline');
    component.toggleTimeline();
    expect(component.viewMode).toBe('list');
  });
});
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: FAIL to compile — `BibleComponent` does not exist yet.

- [ ] **Step 3: Write the component**

`web/src/app/bible/bible.component.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto, BeatDto, EpisodeDto } from '../core/models';

@Component({
  selector: 'app-bible',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bible.component.html',
})
export class BibleComponent implements OnInit {
  episodes: EpisodeDto[] = [];
  selectedEpisodeId: number | null = null;
  beats: BeatDto[] = [];
  assets: AssetDto[] = [];
  viewMode: 'list' | 'timeline' = 'list';

  constructor(private readonly api: ApiClientService) {}

  ngOnInit(): void {
    this.api.getProjects().subscribe((projects) => {
      const project = projects[0];
      if (!project) return;
      this.api.getEpisodes(project.id).subscribe((episodes) => {
        this.episodes = episodes;
        if (episodes.length > 0) {
          this.selectEpisode(episodes[0].id);
        }
      });
    });
  }

  selectEpisode(episodeId: number): void {
    this.selectedEpisodeId = episodeId;
    this.api.getBeats(episodeId).subscribe((beats) => (this.beats = beats));
    this.api.getAssets(episodeId).subscribe((assets) => (this.assets = assets));
  }

  assetsForBeat(beat: BeatDto): AssetDto[] {
    return this.assets.filter((asset) => beat.assetIds.includes(asset.id));
  }

  get unassignedAssets(): AssetDto[] {
    const linkedIds = new Set(this.beats.flatMap((beat) => beat.assetIds));
    return this.assets.filter((asset) => !linkedIds.has(asset.id));
  }

  toggleTimeline(): void {
    this.viewMode = this.viewMode === 'list' ? 'timeline' : 'list';
  }
}
```

- [ ] **Step 4: Write the template**

`web/src/app/bible/bible.component.html`:

```html
<div class="bible-view">
  <div class="episode-picker">
    <label for="episode-select">Episode</label>
    <select id="episode-select" [ngModel]="selectedEpisodeId" (ngModelChange)="selectEpisode($event)">
      <option *ngFor="let ep of episodes" [ngValue]="ep.id">{{ ep.name }}</option>
    </select>
    <button type="button" (click)="toggleTimeline()">
      {{ viewMode === 'list' ? 'Show Timeline' : 'Show List' }}
    </button>
  </div>

  <ng-container *ngIf="viewMode === 'list'">
    <section class="beat" *ngFor="let beat of beats">
      <h3>{{ beat.timecode }} — {{ beat.purpose }}</h3>
      <ul>
        <li *ngFor="let asset of assetsForBeat(beat)">
          <strong>{{ asset.code }}</strong> — {{ asset.title }} ({{ asset.assetTypeName }}, {{ asset.status }})
        </li>
      </ul>
    </section>
    <section class="beat unassigned" *ngIf="unassignedAssets.length > 0">
      <h3>Unassigned</h3>
      <ul>
        <li *ngFor="let asset of unassignedAssets">
          <strong>{{ asset.code }}</strong> — {{ asset.title }}
        </li>
      </ul>
    </section>
  </ng-container>
</div>
```

Task 11 replaces the bare `*ngIf="viewMode === 'list'"` container with an `else` branch rendering the timeline component — this task deliberately leaves the `viewMode === 'timeline'` case rendering nothing, since Task 11 doesn't exist yet.

- [ ] **Step 5: Run the test to verify it passes**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: PASS, 4 tests.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Add Bible view component (episode/timecode order, grouped by beat)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 11: Timeline display mode (vertical stacked tracks)

**Files:**
- Create: `web/src/app/bible/timeline-view.component.ts`
- Create: `web/src/app/bible/timeline-view.component.html`
- Test: `web/src/app/bible/timeline-view.component.spec.ts`
- Modify: `web/src/app/bible/bible.component.ts` (import `TimelineViewComponent`)
- Modify: `web/src/app/bible/bible.component.html` (render it when `viewMode === 'timeline'`)

**Interfaces:**
- Consumes: `BeatDto[]`, `AssetDto[]` (Task 9's models), as `@Input()`s.
- Produces: `TimelineViewComponent` with a `lanes: { name: string; assets: AssetDto[] }[]` computed field. Lane assignment is keyed by `AssetDto.assetTypeName`: `PieceToCamera` → `A-Roll`, `Shot` → `B-Roll`, `Animation` → `Animations`, `Title` → `Titles`, anything else → `Other`. Within a lane, assets are ordered by their position in the flattened beat/timecode sequence (`beats.flatMap(b => b.assetIds)`), so the lane reads left-to-right in story order — the same reason a DaVinci timeline reads left-to-right by playhead position.

- [ ] **Step 1: Write the failing component test**

`web/src/app/bible/timeline-view.component.spec.ts`:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TimelineViewComponent } from './timeline-view.component';
import { AssetDto, BeatDto } from '../core/models';

describe('TimelineViewComponent', () => {
  let fixture: ComponentFixture<TimelineViewComponent>;
  let component: TimelineViewComponent;

  function asset(id: number, assetTypeName: string, code: string): AssetDto {
    return {
      id, episodeId: 1, assetTypeId: 1, assetTypeName, code, title: code,
      scriptText: null, status: 'Planned', notes: null, sequenceNumber: null,
      targetLengthSeconds: null, completedAtUtc: null, attributes: {}, beatIds: [],
    };
  }

  const beats: BeatDto[] = [
    { id: 1, episodeId: 1, timecode: '00:00', purpose: 'Cold open', assetIds: [2, 1] },
    { id: 2, episodeId: 1, timecode: '02:00', purpose: 'Graphic', assetIds: [3] },
  ];
  const assets: AssetDto[] = [
    asset(1, 'Shot', 'A-01'),
    asset(2, 'PieceToCamera', 'E-S'),
    asset(3, 'Animation', 'g1_gears'),
    asset(4, 'Title', 't1_title'),
    asset(5, 'Flyover', 'f1'),
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TimelineViewComponent] }).compileComponents();
    fixture = TestBed.createComponent(TimelineViewComponent);
    component = fixture.componentInstance;
    component.beats = beats;
    component.assets = assets;
    component.ngOnChanges();
    fixture.detectChanges();
  });

  it('groups assets into lanes by asset type', () => {
    const laneNames = component.lanes.map((lane) => lane.name);
    expect(laneNames).toEqual(['A-Roll', 'B-Roll', 'Animations', 'Titles', 'Other']);
  });

  it('orders assets within the B-Roll lane by beat sequence, not asset id', () => {
    const bRoll = component.lanes.find((lane) => lane.name === 'B-Roll')!;
    expect(bRoll.assets.map((a) => a.code)).toEqual(['A-01']);
  });

  it('orders the A-Roll lane correctly when the beat lists the asset before others', () => {
    const aRoll = component.lanes.find((lane) => lane.name === 'A-Roll')!;
    expect(aRoll.assets.map((a) => a.code)).toEqual(['E-S']);
  });

  it('puts an asset type with no lane mapping into Other', () => {
    const other = component.lanes.find((lane) => lane.name === 'Other')!;
    expect(other.assets.map((a) => a.code)).toEqual(['f1']);
  });
});
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: FAIL to compile — `TimelineViewComponent` does not exist yet.

- [ ] **Step 3: Write the component**

`web/src/app/bible/timeline-view.component.ts`:

```typescript
import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssetDto, BeatDto } from '../core/models';

interface TimelineLane {
  name: string;
  assets: AssetDto[];
}

const LANE_BY_ASSET_TYPE: Record<string, string> = {
  PieceToCamera: 'A-Roll',
  Shot: 'B-Roll',
  Animation: 'Animations',
  Title: 'Titles',
};

const LANE_ORDER = ['A-Roll', 'B-Roll', 'Animations', 'Titles', 'Other'];

@Component({
  selector: 'app-timeline-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './timeline-view.component.html',
})
export class TimelineViewComponent implements OnChanges {
  @Input() beats: BeatDto[] = [];
  @Input() assets: AssetDto[] = [];

  lanes: TimelineLane[] = [];

  ngOnChanges(): void {
    this.lanes = this.buildLanes();
  }

  private buildLanes(): TimelineLane[] {
    const orderedAssetIds = this.beats.flatMap((beat) => beat.assetIds);
    const orderIndex = new Map(orderedAssetIds.map((id, index) => [id, index]));

    const grouped = new Map<string, AssetDto[]>();
    for (const asset of this.assets) {
      const laneName = LANE_BY_ASSET_TYPE[asset.assetTypeName] ?? 'Other';
      if (!grouped.has(laneName)) grouped.set(laneName, []);
      grouped.get(laneName)!.push(asset);
    }

    for (const list of grouped.values()) {
      list.sort((a, b) => {
        const aIndex = orderIndex.has(a.id) ? orderIndex.get(a.id)! : Number.MAX_SAFE_INTEGER;
        const bIndex = orderIndex.has(b.id) ? orderIndex.get(b.id)! : Number.MAX_SAFE_INTEGER;
        return aIndex - bIndex;
      });
    }

    return LANE_ORDER.filter((name) => grouped.has(name)).map((name) => ({
      name,
      assets: grouped.get(name)!,
    }));
  }
}
```

- [ ] **Step 4: Write the template**

`web/src/app/bible/timeline-view.component.html`:

```html
<div class="timeline-view">
  <div class="lane" *ngFor="let lane of lanes">
    <h4>{{ lane.name }}</h4>
    <div class="lane-track">
      <div class="clip" *ngFor="let asset of lane.assets">
        <strong>{{ asset.code }}</strong>
        <span>{{ asset.title }}</span>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 5: Run the test to verify it passes**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: PASS, 4 tests.

- [ ] **Step 6: Wire the toggle into `BibleComponent`**

Modify `web/src/app/bible/bible.component.ts` — add the import and register it in the standalone `imports` array:

```typescript
import { TimelineViewComponent } from './timeline-view.component';
```

```typescript
@Component({
  selector: 'app-bible',
  standalone: true,
  imports: [CommonModule, FormsModule, TimelineViewComponent],
  templateUrl: './bible.component.html',
})
```

Modify `web/src/app/bible/bible.component.html` — replace the closing `</ng-container>` block's bare `*ngIf` with an `else` branch:

```html
  <ng-container *ngIf="viewMode === 'list'; else timelineTpl">
    <section class="beat" *ngFor="let beat of beats">
      <h3>{{ beat.timecode }} — {{ beat.purpose }}</h3>
      <ul>
        <li *ngFor="let asset of assetsForBeat(beat)">
          <strong>{{ asset.code }}</strong> — {{ asset.title }} ({{ asset.assetTypeName }}, {{ asset.status }})
        </li>
      </ul>
    </section>
    <section class="beat unassigned" *ngIf="unassignedAssets.length > 0">
      <h3>Unassigned</h3>
      <ul>
        <li *ngFor="let asset of unassignedAssets">
          <strong>{{ asset.code }}</strong> — {{ asset.title }}
        </li>
      </ul>
    </section>
  </ng-container>
  <ng-template #timelineTpl>
    <app-timeline-view [beats]="beats" [assets]="assets"></app-timeline-view>
  </ng-template>
```

- [ ] **Step 7: Run the full Angular test suite to verify nothing broke**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: PASS, all tests from Tasks 9-11.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add Timeline display mode as a toggle within the Bible view

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 12: Production Plan view component (shoot order, grouped by phase)

Unlike Bible view (Task 10), which is scoped to one episode, Production Plan is scoped to the whole project — shoot order groups shots from different episodes together by physical phase/setup (e.g. Phase 1 "The software time machine" is an EP2 shot filmed first). So this component fetches every episode's assets and merges them client-side with `forkJoin`, rather than reusing `BibleComponent`'s single-episode pattern.

**Files:**
- Create: `web/src/app/production-plan/production-plan.component.ts`
- Create: `web/src/app/production-plan/production-plan.component.html`
- Test: `web/src/app/production-plan/production-plan.component.spec.ts`

**Interfaces:**
- Consumes: `ApiClientService.getProjects()`, `.getEpisodes()`, `.getAssets()` (Task 9).
- Produces: `ProductionPlanComponent` with a `groups: { phase: string; assets: AssetDto[] }[]` field, sorted by `AssetDto.sequenceNumber` within each group, grouped by the `PhaseGroup` key in `AssetDto.attributes` (falling back to `'Unphased'` when absent — an asset the importer didn't attach a phase to, or one created later via the API/MCP without one).

- [ ] **Step 1: Write the failing component test**

`web/src/app/production-plan/production-plan.component.spec.ts`:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ProductionPlanComponent } from './production-plan.component';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto, EpisodeDto, ProjectDto } from '../core/models';

describe('ProductionPlanComponent', () => {
  let fixture: ComponentFixture<ProductionPlanComponent>;
  let component: ProductionPlanComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode1: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const episode2: EpisodeDto = { id: 20, projectId: 1, name: 'EP2', orderIndex: 2 };

  function asset(id: number, code: string, sequenceNumber: number, phase: string): AssetDto {
    return {
      id, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code, title: code,
      scriptText: null, status: 'Planned', notes: null, sequenceNumber, targetLengthSeconds: null,
      completedAtUtc: null, attributes: { PhaseGroup: phase }, beatIds: [],
    };
  }

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getProjects', 'getEpisodes', 'getAssets']);
    apiSpy.getProjects.and.returnValue(of([project]));
    apiSpy.getEpisodes.and.returnValue(of([episode1, episode2]));
    apiSpy.getAssets.and.callFake((episodeId: number) =>
      episodeId === 20
        ? of([asset(2, 'F-01', 1, 'Phase 1: The software time machine')])
        : of([asset(1, 'B-02', 2, 'Phase 2: Makerspace trip')]));

    await TestBed.configureTestingModule({
      imports: [ProductionPlanComponent],
      providers: [{ provide: ApiClientService, useValue: apiSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductionPlanComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('merges assets from every episode in the project', () => {
    const allCodes = component.groups.flatMap((g) => g.assets.map((a) => a.code));
    expect(allCodes).toEqual(['F-01', 'B-02']);
  });

  it('orders phase groups by the lowest sequence number in that phase', () => {
    expect(component.groups.map((g) => g.phase)).toEqual([
      'Phase 1: The software time machine',
      'Phase 2: Makerspace trip',
    ]);
  });

  it('falls back to Unphased when an asset has no PhaseGroup attribute', () => {
    apiSpy.getAssets.and.returnValue(of([{
      id: 3, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'X-01', title: 'X-01',
      scriptText: null, status: 'Planned', notes: null, sequenceNumber: 1, targetLengthSeconds: null,
      completedAtUtc: null, attributes: {}, beatIds: [],
    }]));

    component.ngOnInit();

    expect(component.groups.some((g) => g.phase === 'Unphased')).toBeTrue();
  });
});
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: FAIL to compile — `ProductionPlanComponent` does not exist yet.

- [ ] **Step 3: Write the component**

`web/src/app/production-plan/production-plan.component.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto } from '../core/models';

interface PhaseGroup {
  phase: string;
  assets: AssetDto[];
}

@Component({
  selector: 'app-production-plan',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './production-plan.component.html',
})
export class ProductionPlanComponent implements OnInit {
  groups: PhaseGroup[] = [];

  constructor(private readonly api: ApiClientService) {}

  ngOnInit(): void {
    this.api.getProjects().subscribe((projects) => {
      const project = projects[0];
      if (!project) return;

      this.api.getEpisodes(project.id).subscribe((episodes) => {
        if (episodes.length === 0) {
          this.groups = [];
          return;
        }

        forkJoin(episodes.map((episode) => this.api.getAssets(episode.id))).subscribe((assetLists) => {
          this.groups = this.buildGroups(assetLists.flat());
        });
      });
    });
  }

  private buildGroups(assets: AssetDto[]): PhaseGroup[] {
    const sorted = [...assets].sort(
      (a, b) => (a.sequenceNumber ?? Number.MAX_SAFE_INTEGER) - (b.sequenceNumber ?? Number.MAX_SAFE_INTEGER));

    const map = new Map<string, AssetDto[]>();
    for (const asset of sorted) {
      const phase = asset.attributes['PhaseGroup'] ?? 'Unphased';
      if (!map.has(phase)) map.set(phase, []);
      map.get(phase)!.push(asset);
    }

    return Array.from(map.entries()).map(([phase, assets]) => ({ phase, assets }));
  }
}
```

- [ ] **Step 4: Write the template**

`web/src/app/production-plan/production-plan.component.html`:

```html
<div class="production-plan-view">
  <section class="phase-group" *ngFor="let group of groups">
    <h3>{{ group.phase }}</h3>
    <table>
      <thead>
        <tr>
          <th>Seq</th><th>Code</th><th>Title</th><th>Location</th>
          <th>Angle &amp; camera</th><th>Audio</th><th>Status</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let asset of group.assets">
          <td>{{ asset.sequenceNumber }}</td>
          <td>{{ asset.code }}</td>
          <td>{{ asset.title }}</td>
          <td>{{ asset.attributes['Location'] }}</td>
          <td>{{ asset.attributes['AngleAndCamera'] }}</td>
          <td>{{ asset.attributes['AudioNotes'] }}</td>
          <td>{{ asset.status }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</div>
```

- [ ] **Step 5: Run the test to verify it passes**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: PASS, 3 tests.

- [ ] **Step 6: Wire up routing between the two views**

`web/src/app/app.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { BibleComponent } from './bible/bible.component';
import { ProductionPlanComponent } from './production-plan/production-plan.component';

export const routes: Routes = [
  { path: '', redirectTo: 'bible', pathMatch: 'full' },
  { path: 'bible', component: BibleComponent },
  { path: 'production-plan', component: ProductionPlanComponent },
];
```

Replace the generated `web/src/app/app.component.html` with a simple nav shell:

```html
<nav>
  <a routerLink="/bible">Bible</a>
  <a routerLink="/production-plan">Production Plan</a>
</nav>
<router-outlet></router-outlet>
```

And add `RouterLink` to `app.component.ts`'s standalone `imports` array (alongside the existing `RouterOutlet`).

- [ ] **Step 7: Run the full Angular test suite to verify nothing broke**

```bash
cd web
npx ng test --watch=false
cd ..
```

Expected: PASS, all tests from Tasks 9-12.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add Production Plan view component and wire up routing

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 13: Importer — `StoryboardHtmlParser`

Parses two things out of `storyboard.html`: the per-setup shot tables (Setup A–F, in "02 The shot list") and the animation/title clip tables (in "04 Animations, delivered"). It deliberately does **not** attempt to parse the dense per-episode beat-sheet summary row (e.g. `<tr><td><strong>EP 1</strong></td><td><code>00:00</code> A-01/02/03, B-01 &middot; ...</td></tr>`) — that text mixes shot-code ranges, freeform prose, and inconsistent code formats (`A-01/02/03`, `G1`, `f1`, `E-S`) too unreliably to parse safely for one-time seed data. Beats are derived instead from `production_plan.md`'s much more regular per-shot meta line (Task 14) — a deliberate, documented Phase 1 scope decision, not an oversight.

**Known limitation to verify during implementation:** this parser assumes every Setup A–F table shares the same 3-column shape (`Shot | Scene setup | Camera & capture`), confirmed for Setup A. If Setup E ("Pieces to camera") or Setup F ("Manufactured failures") use a different column layout in the live file, rows from those tables will be silently skipped (the code checks `tds.Count < 3` and skips short rows rather than throwing) rather than corrupt data. Check `storyboard.html`'s Setup E/F tables against this assumption when running the importer for real, and adjust column indices if they differ.

**Files:**
- Create: `src/ProductionBible.Importer/Models/ParsedShotRow.cs`
- Create: `src/ProductionBible.Importer/Models/ParsedAnimationRow.cs`
- Create: `src/ProductionBible.Importer/StoryboardHtmlParser.cs`
- Create: `tests/ProductionBible.Importer.Tests/Fixtures/storyboard.html` (verbatim copy)
- Test: `tests/ProductionBible.Importer.Tests/StoryboardHtmlParserTests.cs`

**Interfaces:**
- Produces: `StoryboardHtmlParser.Parse(string html)` returning `(List<ParsedShotRow> ShotRows, List<ParsedAnimationRow> AnimationRows)`. `ParsedShotRow(string Code, string EpisodeTimecodeRaw, string SceneSetup, string CaptureNote, string SetupSection)`. `ParsedAnimationRow(string Code, int? DurationSeconds, string Description)`. Task 15 (`ImportMapper`) consumes both lists by exactly these property names.

- [ ] **Step 1: Copy the real file as a test fixture**

```bash
mkdir -p tests/ProductionBible.Importer.Tests/Fixtures
cp "D:\Data\source\HalfNutELS-Video\storyboard.html" tests/ProductionBible.Importer.Tests/Fixtures/storyboard.html
```

Edit `tests/ProductionBible.Importer.Tests/ProductionBible.Importer.Tests.csproj` to copy fixtures to the test output directory — add inside the existing `<Project>` element:

```xml
  <ItemGroup>
    <None Include="Fixtures\**" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

- [ ] **Step 2: Write the model records**

`src/ProductionBible.Importer/Models/ParsedShotRow.cs`:

```csharp
namespace ProductionBible.Importer.Models;

public record ParsedShotRow(
    string Code,
    string EpisodeTimecodeRaw,
    string SceneSetup,
    string CaptureNote,
    string SetupSection);
```

`src/ProductionBible.Importer/Models/ParsedAnimationRow.cs`:

```csharp
namespace ProductionBible.Importer.Models;

public record ParsedAnimationRow(string Code, int? DurationSeconds, string Description);
```

- [ ] **Step 3: Write the failing parser test**

`tests/ProductionBible.Importer.Tests/StoryboardHtmlParserTests.cs`:

```csharp
using ProductionBible.Importer;

namespace ProductionBible.Importer.Tests;

public class StoryboardHtmlParserTests
{
    private static string LoadFixture() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "storyboard.html"));

    [Fact]
    public void Parses_the_A01_shot_row_from_the_real_file()
    {
        var (shotRows, _) = StoryboardHtmlParser.Parse(LoadFixture());

        var a01 = Assert.Single(shotRows, r => r.Code == "A-01");
        Assert.Equal("EP1 00:00", a01.EpisodeTimecodeRaw);
        Assert.Contains("Steel bar", a01.SceneSetup);
        Assert.Contains("3-jaw", a01.SceneSetup);
        Assert.Contains("Macro on the tool entering the work", a01.CaptureNote);
        Assert.Equal("Setup A — Lathe, running", a01.SetupSection);
    }

    [Fact]
    public void Parses_shot_rows_from_multiple_setup_sections()
    {
        var (shotRows, _) = StoryboardHtmlParser.Parse(LoadFixture());

        var codes = shotRows.Select(r => r.Code).ToHashSet();
        Assert.Contains("A-01", codes);
        Assert.Contains("B-02", codes);
        Assert.Contains("C-01", codes);
        Assert.Contains("D-01", codes);
        Assert.Contains("F-01", codes);
        Assert.True(shotRows.Count >= 40, $"expected at least 40 shot rows, got {shotRows.Count}");
    }

    [Fact]
    public void Parses_a_single_code_animation_row_with_its_duration()
    {
        var (_, animationRows) = StoryboardHtmlParser.Parse(LoadFixture());

        var g1 = Assert.Single(animationRows, r => r.Code == "g1_gears");
        Assert.Equal(15, g1.DurationSeconds);
        Assert.Contains("gear train dissolving", g1.Description);
    }

    [Fact]
    public void Expands_the_condensed_t1_through_t5_title_row_into_five_rows()
    {
        var (_, animationRows) = StoryboardHtmlParser.Parse(LoadFixture());

        var titleCodes = new[] { "t1_title", "t2_title", "t3_title", "t4_title", "t5_title" };
        foreach (var code in titleCodes)
        {
            var row = Assert.Single(animationRows, r => r.Code == code);
            Assert.Equal(4, row.DurationSeconds);
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

```bash
dotnet test tests/ProductionBible.Importer.Tests --filter StoryboardHtmlParserTests
```

Expected: FAIL to compile — `StoryboardHtmlParser` does not exist yet.

- [ ] **Step 5: Write the parser**

`src/ProductionBible.Importer/StoryboardHtmlParser.cs`:

```csharp
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ProductionBible.Importer.Models;

namespace ProductionBible.Importer;

public static class StoryboardHtmlParser
{
    public static (List<ParsedShotRow> ShotRows, List<ParsedAnimationRow> AnimationRows) Parse(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return (ParseShotRows(doc), ParseAnimationRows(doc));
    }

    private static List<ParsedShotRow> ParseShotRows(HtmlDocument doc)
    {
        var rows = new List<ParsedShotRow>();
        var setupHeadings = doc.DocumentNode.SelectNodes("//h3")?
            .Where(h => Decode(h.InnerText).TrimStart().StartsWith("Setup ", StringComparison.Ordinal))
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var heading in setupHeadings)
        {
            var setupName = Decode(heading.InnerText).Trim();
            var tableWrap = heading.SelectSingleNode("following-sibling::div[contains(@class,'tablewrap')][1]");
            var trs = tableWrap?.SelectNodes(".//tbody/tr");
            if (trs is null) continue;

            foreach (var tr in trs)
            {
                var tds = tr.SelectNodes("td");
                if (tds is null || tds.Count < 3) continue;

                var strong = tds[0].SelectSingleNode(".//strong");
                if (strong is null) continue;
                var span = tds[0].SelectSingleNode(".//span[contains(@class,'small')]");

                rows.Add(new ParsedShotRow(
                    Code: Decode(strong.InnerText).Trim(),
                    EpisodeTimecodeRaw: span is null ? "" : Decode(span.InnerText).Trim(),
                    SceneSetup: Decode(tds[1].InnerText).Trim(),
                    CaptureNote: Decode(tds[2].InnerText).Trim(),
                    SetupSection: setupName));
            }
        }

        return rows;
    }

    private static List<ParsedAnimationRow> ParseAnimationRows(HtmlDocument doc)
    {
        var results = new List<ParsedAnimationRow>();
        var candidateRows = doc.DocumentNode
            .SelectNodes("//table/tbody/tr[td[1]/code and td[2][contains(@class,'num-col')]]")
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var tr in candidateRows)
        {
            var tds = tr.SelectNodes("td");
            var codeCellText = Decode(tds[0].InnerText).Trim();
            var durationText = Decode(tds[1].InnerText).Trim();
            var description = Decode(tds[2].InnerText).Trim();
            var durationSeconds = ParseDurationSeconds(durationText);

            foreach (var code in ExpandCodeRange(codeCellText))
            {
                results.Add(new ParsedAnimationRow(code, durationSeconds, description));
            }
        }

        return results;
    }

    private static string Decode(string html) => HtmlEntity.DeEntitize(html);

    private static int? ParseDurationSeconds(string text)
    {
        var match = Regex.Match(text, @"([\d.]+)\s*s");
        return match.Success && double.TryParse(match.Groups[1].Value, out var seconds)
            ? (int)Math.Round(seconds)
            : null;
    }

    private static IEnumerable<string> ExpandCodeRange(string codeCellText)
    {
        var match = Regex.Match(
            codeCellText,
            @"^(?<pre>[a-zA-Z]+)(?<from>\d+)(?<suf>_[a-zA-Z]+)\s*(?:…|\.\.\.)\s*[a-zA-Z]+(?<to>\d+)_[a-zA-Z]+$");
        if (!match.Success)
        {
            yield return codeCellText;
            yield break;
        }

        var from = int.Parse(match.Groups["from"].Value);
        var to = int.Parse(match.Groups["to"].Value);
        for (var i = from; i <= to; i++)
        {
            yield return $"{match.Groups["pre"].Value}{i}{match.Groups["suf"].Value}";
        }
    }
}
```

- [ ] **Step 6: Run the test to verify it passes**

```bash
dotnet test tests/ProductionBible.Importer.Tests --filter StoryboardHtmlParserTests
```

Expected: PASS, 4 tests. If `Parses_shot_rows_from_multiple_setup_sections` fails because a Setup E or F table has a different column count than assumed, that confirms the "known limitation" above — investigate that table's actual HTML structure and adjust `ParseShotRows`'s column indices (or add a second parsing branch) accordingly before moving on; don't silently loosen the test's assertions to paper over it.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Add StoryboardHtmlParser with real-file fixture tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 14: Importer — `ProductionPlanMarkdownParser`

Parses each `### CODE — Title` shot page in `production_plan.md`. This is the primary source of `SequenceNumber`, `PhaseGroup`, and — critically — the (episode, timecode) pairs Task 15 turns into `Beat` entities, because this file's per-shot meta line (`Sequence N of 65 · Phase X: Name · EPn TIMECODE [· TIMECODE...]`) is far more regular than `storyboard.html`'s dense summary row (see Task 13's rationale for not parsing that row at all).

**Files:**
- Create: `src/ProductionBible.Importer/Models/ParsedShotPage.cs`
- Create: `src/ProductionBible.Importer/ProductionPlanMarkdownParser.cs`
- Create: `tests/ProductionBible.Importer.Tests/Fixtures/production_plan.md` (verbatim copy)
- Test: `tests/ProductionBible.Importer.Tests/ProductionPlanMarkdownParserTests.cs`

**Interfaces:**
- Produces: `ProductionPlanMarkdownParser.Parse(string markdown)` returning `List<ParsedShotPage>`. `ParsedShotPage(string Code, string Title, int SequenceNumber, string PhaseGroup, int EpisodeNumber, List<string> Timecodes, string? Location, string? SceneSetup, string? AngleAndCamera, string? AudioNotes, string? TargetLengthRaw, string? ScriptText, string? AdditionalConsiderations)`. Task 15 consumes this by exactly these property names, alongside `ParsedShotRow`/`ParsedAnimationRow` from Task 13.

**Known limitation to verify during implementation:** this parser assumes every shot page names exactly one episode (`EP2 00:00 · 22:30` — one `EP`, one or more timecodes within it). Every page inspected so far fits this pattern; if a real page ever spans two different episodes, only the first `EP` token is captured — that page's Beats would all land on the wrong episode's timecodes for any timecode after the first `EP` switch. Not expected to occur given the shooting-order structure, but flagged rather than silently assumed correct.

- [ ] **Step 1: Copy the real file as a test fixture**

```bash
cp "D:\Data\source\HalfNutELS-Video\production_plan.md" tests/ProductionBible.Importer.Tests/Fixtures/production_plan.md
```

(The `.csproj` change from Task 13 Step 1 already copies everything under `Fixtures/` to the output directory, so no further project-file edit is needed.)

- [ ] **Step 2: Write the model record**

`src/ProductionBible.Importer/Models/ParsedShotPage.cs`:

```csharp
namespace ProductionBible.Importer.Models;

public record ParsedShotPage(
    string Code,
    string Title,
    int SequenceNumber,
    string PhaseGroup,
    int EpisodeNumber,
    List<string> Timecodes,
    string? Location,
    string? SceneSetup,
    string? AngleAndCamera,
    string? AudioNotes,
    string? TargetLengthRaw,
    string? ScriptText,
    string? AdditionalConsiderations);
```

- [ ] **Step 3: Write the failing parser test**

`tests/ProductionBible.Importer.Tests/ProductionPlanMarkdownParserTests.cs`:

```csharp
using ProductionBible.Importer;

namespace ProductionBible.Importer.Tests;

public class ProductionPlanMarkdownParserTests
{
    private static string LoadFixture() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "production_plan.md"));

    [Fact]
    public void Parses_the_F01_shot_page_fields_from_the_real_file()
    {
        var pages = ProductionPlanMarkdownParser.Parse(LoadFixture());

        var f01 = Assert.Single(pages, p => p.Code == "F-01");
        Assert.Equal("The software time machine", f01.Title);
        Assert.Equal(1, f01.SequenceNumber);
        Assert.Equal("Phase 1: The software time machine", f01.PhaseGroup);
        Assert.Equal(2, f01.EpisodeNumber);
        Assert.Equal(new List<string> { "00:00", "22:30" }, f01.Timecodes);
        Assert.Equal("Home, at the lathe", f01.Location);
        Assert.Contains("Setup A (running)", f01.SceneSetup);
        Assert.Contains("Macro on the first 30 mm", f01.AngleAndCamera);
        Assert.Contains("Clean cutting sound", f01.AudioNotes);
        Assert.Contains("10-15 min raw", f01.TargetLengthRaw);
        Assert.Contains("No dialogue during the cut itself", f01.ScriptText);
        Assert.Contains("This is not literally the commit before", f01.AdditionalConsiderations);
    }

    [Fact]
    public void Parses_all_65_shot_pages()
    {
        var pages = ProductionPlanMarkdownParser.Parse(LoadFixture());

        Assert.Equal(65, pages.Count);
        Assert.Equal(65, pages.Select(p => p.Code).Distinct().Count());
    }

    [Fact]
    public void Every_page_has_a_positive_sequence_number_and_at_least_one_timecode()
    {
        var pages = ProductionPlanMarkdownParser.Parse(LoadFixture());

        Assert.All(pages, p =>
        {
            Assert.True(p.SequenceNumber > 0, $"{p.Code} has non-positive sequence number");
            Assert.NotEmpty(p.Timecodes);
        });
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

```bash
dotnet test tests/ProductionBible.Importer.Tests --filter ProductionPlanMarkdownParserTests
```

Expected: FAIL to compile — `ProductionPlanMarkdownParser` does not exist yet.

- [ ] **Step 5: Write the parser**

`src/ProductionBible.Importer/ProductionPlanMarkdownParser.cs`:

```csharp
using System.Text.RegularExpressions;
using ProductionBible.Importer.Models;

namespace ProductionBible.Importer;

public static class ProductionPlanMarkdownParser
{
    public static List<ParsedShotPage> Parse(string markdown)
    {
        var pages = new List<ParsedShotPage>();
        var chunks = Regex.Split(markdown, @"(?=^### )", RegexOptions.Multiline);

        foreach (var chunk in chunks)
        {
            if (!chunk.TrimStart().StartsWith("### ", StringComparison.Ordinal)) continue;

            var headingMatch = Regex.Match(chunk, @"^###\s+(?<code>\S+)\s+—\s+(?<title>.+?)\s*$", RegexOptions.Multiline);
            if (!headingMatch.Success) continue;

            var metaMatch = Regex.Match(
                chunk,
                @"<p class=""meta"">Sequence (?<seq>\d+) of \d+ &nbsp;&middot;&nbsp; Phase (?<phaseNum>\d+): (?<phaseName>[^&]+?) &nbsp;&middot;&nbsp; (?<timecodes>.+?)</p>");
            if (!metaMatch.Success) continue;

            var epMatch = Regex.Match(metaMatch.Groups["timecodes"].Value, @"EP(?<ep>\d+)\s+(?<times>.+)$");
            var timecodes = epMatch.Success
                ? Regex.Split(epMatch.Groups["times"].Value, @"\s*&middot;\s*")
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .ToList()
                : new List<string>();
            var episodeNumber = epMatch.Success ? int.Parse(epMatch.Groups["ep"].Value) : 0;

            var fields = ParseFieldTable(chunk);

            pages.Add(new ParsedShotPage(
                Code: headingMatch.Groups["code"].Value.Trim(),
                Title: headingMatch.Groups["title"].Value.Trim(),
                SequenceNumber: int.Parse(metaMatch.Groups["seq"].Value),
                PhaseGroup: $"Phase {metaMatch.Groups["phaseNum"].Value}: {metaMatch.Groups["phaseName"].Value.Trim()}",
                EpisodeNumber: episodeNumber,
                Timecodes: timecodes,
                Location: fields.GetValueOrDefault("location"),
                SceneSetup: fields.GetValueOrDefault("setup"),
                AngleAndCamera: fields.GetValueOrDefault("angle & camera"),
                AudioNotes: fields.GetValueOrDefault("audio to capture"),
                TargetLengthRaw: fields.GetValueOrDefault("target length"),
                ScriptText: ParseSection(chunk, "#### Script", ">"),
                AdditionalConsiderations: ParseSection(chunk, "#### Additional considerations", "-")));
        }

        return pages;
    }

    private static Dictionary<string, string> ParseFieldTable(string chunk)
    {
        var fields = new Dictionary<string, string>();
        foreach (Match m in Regex.Matches(
            chunk, @"^\|\s*\*\*(?<key>[^*]+)\*\*\s*\|\s*(?<value>.*?)\s*\|\s*$", RegexOptions.Multiline))
        {
            fields[m.Groups["key"].Value.Trim().ToLowerInvariant()] = m.Groups["value"].Value.Trim();
        }
        return fields;
    }

    private static string? ParseSection(string chunk, string heading, string linePrefix)
    {
        var startIndex = chunk.IndexOf(heading, StringComparison.Ordinal);
        if (startIndex < 0) return null;

        var afterHeading = chunk[(startIndex + heading.Length)..];
        var nextHeadingMatch = Regex.Match(afterHeading, @"^####\s", RegexOptions.Multiline);
        var section = nextHeadingMatch.Success ? afterHeading[..nextHeadingMatch.Index] : afterHeading;

        var lines = section.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith(linePrefix, StringComparison.Ordinal))
            .Select(l => l.TrimStart(linePrefix[0]).Trim())
            .ToList();

        if (lines.Count == 0) return null;
        return linePrefix == ">" ? string.Join(" ", lines) : string.Join("\n", lines.Select(l => $"- {l}"));
    }
}
```

- [ ] **Step 6: Run the test to verify it passes**

```bash
dotnet test tests/ProductionBible.Importer.Tests --filter ProductionPlanMarkdownParserTests
```

Expected: PASS, 3 tests. If `Parses_all_65_shot_pages` reports a different count, that's real signal — either the live file's page count has changed since the spec was written (re-check `production_plan.md`'s own "Sequence N of 65" numbers) or the heading/meta regex is missing a page with slightly different formatting; inspect the actual mismatch rather than adjusting the expected `65` to make the test pass.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Add ProductionPlanMarkdownParser with real-file fixture tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---

### Task 15: Importer — `ImportMapper` and console entry point

Orchestrates Tasks 13-14's parsers into one `Project` → `Episode`/`Beat`/`Asset`/`AssetAttribute`/`AssetBeat` graph and saves it. `production_plan.md`'s 65 shot pages are the backbone (they carry `SequenceNumber`, `PhaseGroup`, and the (episode, timecode) pairs that become Beats); `storyboard.html`'s shot rows enrich matching codes with `CaptureNote` and the original `StoryboardMachineConfig`/`StoryboardSetupSection` values (kept as separate attributes from `SceneSetup`, not merged — see the spec's rationale); `storyboard.html`'s animation rows become `Animation`/`Title` assets with no Beat links (Task 13's documented Phase 1 limitation).

**Files:**
- Create: `src/ProductionBible.Importer/ImportMapper.cs`
- Modify: `src/ProductionBible.Importer/Program.cs`
- Test: `tests/ProductionBible.Importer.Tests/ImportMapperTests.cs`

**Interfaces:**
- Consumes: `StoryboardHtmlParser.Parse` (Task 13), `ProductionPlanMarkdownParser.Parse` (Task 14), `ProductionBibleDbContext` (Task 2).
- Produces: `ImportMapper.ImportAsync(string storyboardHtml, string productionPlanMarkdown, string projectName) : Task<Project>` — saves everything and returns the created `Project`.

- [ ] **Step 1: Write the failing end-to-end test**

`tests/ProductionBible.Importer.Tests/ImportMapperTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;

namespace ProductionBible.Importer.Tests;

public class ImportMapperTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    private static string LoadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public async Task Imports_all_65_shot_pages_as_assets_with_a_populated_scene_setup()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var shotPageAssets = await context.Assets
            .Where(a => a.SequenceNumber != null)
            .Include(a => a.Attributes)
            .ToListAsync();
        Assert.Equal(65, shotPageAssets.Count);

        var a01 = Assert.Single(shotPageAssets, a => a.Code == "A-01");
        Assert.Contains(a01.Attributes, attr => attr.Key == "SceneSetup" && attr.Value.Length > 0);
    }

    [Fact]
    public async Task Creates_beats_linking_F01_to_both_of_its_timecodes()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var f01 = await context.Assets
            .Include(a => a.AssetBeats).ThenInclude(ab => ab.Beat)
            .SingleAsync(a => a.Code == "F-01");
        var timecodes = f01.AssetBeats.Select(ab => ab.Beat!.Timecode).OrderBy(t => t).ToList();
        Assert.Equal(new List<string> { "00:00", "22:30" }, timecodes);
    }

    [Fact]
    public async Task Pieces_to_camera_get_a_dedicated_asset_type_distinct_from_shots()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var eS = await context.Assets.Include(a => a.AssetType).SingleAsync(a => a.Code == "E-S");
        var a01 = await context.Assets.Include(a => a.AssetType).SingleAsync(a => a.Code == "A-01");
        Assert.Equal("PieceToCamera", eS.AssetType!.Name);
        Assert.Equal("Shot", a01.AssetType!.Name);
    }

    [Fact]
    public async Task Animation_rows_become_assets_with_no_sequence_number_and_no_beat_links()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var g1 = await context.Assets
            .Include(a => a.AssetBeats)
            .Include(a => a.AssetType)
            .SingleAsync(a => a.Code == "g1_gears");
        Assert.Equal("Animation", g1.AssetType!.Name);
        Assert.Null(g1.SequenceNumber);
        Assert.Empty(g1.AssetBeats);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test tests/ProductionBible.Importer.Tests --filter ImportMapperTests
```

Expected: FAIL to compile — `ImportMapper` does not exist yet.

- [ ] **Step 3: Write `ImportMapper`**

`src/ProductionBible.Importer/ImportMapper.cs`:

```csharp
using System.Text.RegularExpressions;
using ProductionBible.Application.Data;
using ProductionBible.Application.Entities;

namespace ProductionBible.Importer;

public class ImportMapper
{
    private readonly ProductionBibleDbContext _db;
    private readonly Dictionary<int, Episode> _episodesByNumber = new();
    private readonly Dictionary<string, AssetType> _assetTypesByName = new();
    private readonly Dictionary<(int episodeNumber, string timecode), Beat> _beatsByKey = new();

    public ImportMapper(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<Project> ImportAsync(string storyboardHtml, string productionPlanMarkdown, string projectName)
    {
        var (shotRows, animationRows) = StoryboardHtmlParser.Parse(storyboardHtml);
        var shotPages = ProductionPlanMarkdownParser.Parse(productionPlanMarkdown);
        var shotRowsByCode = shotRows.ToDictionary(r => r.Code, r => r);

        var project = new Project { Name = projectName };
        _db.Projects.Add(project);

        var shotType = GetOrCreateAssetType("Shot");
        var pieceToCameraType = GetOrCreateAssetType("PieceToCamera");

        var matchedCodes = new HashSet<string>();
        foreach (var page in shotPages)
        {
            var assetType = page.Code.StartsWith("E-", StringComparison.Ordinal) ? pieceToCameraType : shotType;
            var episode = GetOrCreateEpisode(project, page.EpisodeNumber);

            var asset = new Asset
            {
                Episode = episode,
                AssetType = assetType,
                Code = page.Code,
                Title = page.Title,
                ScriptText = page.ScriptText,
                Status = "Planned",
                SequenceNumber = page.SequenceNumber,
            };

            AddAttribute(asset, "Location", page.Location);
            AddAttribute(asset, "SceneSetup", page.SceneSetup);
            AddAttribute(asset, "AngleAndCamera", page.AngleAndCamera);
            AddAttribute(asset, "AudioNotes", page.AudioNotes);
            AddAttribute(asset, "TargetLengthRaw", page.TargetLengthRaw);
            AddAttribute(asset, "AdditionalConsiderations", page.AdditionalConsiderations);
            AddAttribute(asset, "PhaseGroup", page.PhaseGroup);

            if (shotRowsByCode.TryGetValue(page.Code, out var shotRow))
            {
                matchedCodes.Add(page.Code);
                AddAttribute(asset, "CaptureNote", shotRow.CaptureNote);
                AddAttribute(asset, "StoryboardMachineConfig", shotRow.SceneSetup);
                AddAttribute(asset, "StoryboardSetupSection", shotRow.SetupSection);
            }

            _db.Assets.Add(asset);

            foreach (var timecode in page.Timecodes)
            {
                var beat = GetOrCreateBeat(project, page.EpisodeNumber, timecode, page.Title);
                _db.AssetBeats.Add(new AssetBeat { Asset = asset, Beat = beat });
            }
        }

        foreach (var unmatchedCode in shotRowsByCode.Keys.Except(matchedCodes))
        {
            Console.WriteLine(
                $"Warning: storyboard.html shot row '{unmatchedCode}' has no matching production_plan.md page; skipped.");
        }

        var animationType = GetOrCreateAssetType("Animation");
        var titleType = GetOrCreateAssetType("Title");

        foreach (var row in animationRows)
        {
            var episodeMatch = Regex.Match(row.Description, @"EP\s*(?<ep>\d+)");
            if (!episodeMatch.Success)
            {
                Console.WriteLine($"Warning: animation row '{row.Code}' has no identifiable episode; skipped.");
                continue;
            }

            var episode = GetOrCreateEpisode(project, int.Parse(episodeMatch.Groups["ep"].Value));
            var isTitle = Regex.IsMatch(row.Code, @"^(t\d+_title|l\d+_)");

            var asset = new Asset
            {
                Episode = episode,
                AssetType = isTitle ? titleType : animationType,
                Code = row.Code,
                Title = row.Code,
                Status = "Planned",
                TargetLengthSeconds = row.DurationSeconds,
                Notes = row.Description,
            };
            AddAttribute(asset, "SourceScriptRef", row.Code);
            _db.Assets.Add(asset);
        }

        await _db.SaveChangesAsync();
        return project;
    }

    private Episode GetOrCreateEpisode(Project project, int number)
    {
        if (_episodesByNumber.TryGetValue(number, out var existing)) return existing;
        var episode = new Episode { Project = project, Name = $"EP{number}", OrderIndex = number };
        _episodesByNumber[number] = episode;
        _db.Episodes.Add(episode);
        return episode;
    }

    private AssetType GetOrCreateAssetType(string name)
    {
        if (_assetTypesByName.TryGetValue(name, out var existing)) return existing;
        var type = new AssetType { Name = name };
        _assetTypesByName[name] = type;
        _db.AssetTypes.Add(type);
        return type;
    }

    private Beat GetOrCreateBeat(Project project, int episodeNumber, string timecode, string purpose)
    {
        var key = (episodeNumber, timecode);
        if (_beatsByKey.TryGetValue(key, out var existing)) return existing;
        var episode = GetOrCreateEpisode(project, episodeNumber);
        var beat = new Beat { Episode = episode, Timecode = timecode, Purpose = purpose };
        _beatsByKey[key] = beat;
        _db.Beats.Add(beat);
        return beat;
    }

    private static void AddAttribute(Asset asset, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        asset.Attributes.Add(new AssetAttribute { Asset = asset, Key = key, Value = value });
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
dotnet test tests/ProductionBible.Importer.Tests --filter ImportMapperTests
```

Expected: PASS, 4 tests.

- [ ] **Step 5: Write the console entry point**

`src/ProductionBible.Importer/Program.cs` (replace the template's generated content):

```csharp
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Importer;

if (args.Length < 3)
{
    Console.WriteLine("Usage: ProductionBible.Importer <storyboard.html path> <production_plan.md path> <sqlite db path>");
    return 1;
}

var storyboardHtml = await File.ReadAllTextAsync(args[0]);
var productionPlanMarkdown = await File.ReadAllTextAsync(args[1]);

var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
    .UseSqlite($"Data Source={args[2]}")
    .Options;

await using var db = new ProductionBibleDbContext(options);
await db.Database.MigrateAsync();

var mapper = new ImportMapper(db);
var project = await mapper.ImportAsync(storyboardHtml, productionPlanMarkdown, "HalfNut ELS");

Console.WriteLine($"Imported project '{project.Name}' (id {project.Id}).");
return 0;
```

- [ ] **Step 6: Run the whole solution's test suite**

```bash
dotnet test ProductionBible.sln
```

Expected: PASS — every .NET test from Tasks 1-15.

- [ ] **Step 7: Manually run the importer against the real files into a throwaway database**

```bash
dotnet run --project src/ProductionBible.Importer -- \
  "D:\Data\source\HalfNutELS-Video\storyboard.html" \
  "D:\Data\source\HalfNutELS-Video\production_plan.md" \
  ./halfnutels_seed.db
```

Expected: prints `Imported project 'HalfNut ELS' (id 1).`, plus any `Warning:` lines for unmatched shot rows or unidentifiable animation episodes — read them; they are real signal about what the parsers missed, not noise to suppress. Delete `halfnutels_seed.db` afterward — Task 16 does the real seed import into the app's actual database.

```bash
rm halfnutels_seed.db
```

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add ImportMapper and Importer console entry point

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp"
```

---
