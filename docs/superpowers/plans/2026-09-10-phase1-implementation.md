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
