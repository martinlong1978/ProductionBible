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

    // Task 15 review found a defect in the brief's given ImportMapper code: production_plan.md's 7
    // "pieces to camera" pages with compound codes ("E-L / EP1", "E-B / EP2", etc. — the same base
    // shot filmed separately per episode) were being looked up against storyboard.html's shot rows
    // (keyed on the plain "E-L"/"E-B"/"E-S") using the raw compound page.Code, which can never match.
    // That meant these pages never got enriched with CaptureNote/StoryboardSceneSetup/
    // StoryboardSetupSection, AND their base codes spuriously appeared as "unmatched" shot rows even
    // though a matching production_plan.md page genuinely exists. The fix normalizes " / EPn" off the
    // code before both the Asset.Code assignment and the shotRowsByCode lookup. This test asserts the
    // fix: multiple Asset rows share the plain base Code "E-L" (one per episode), and at least one of
    // them carries the storyboard.html-sourced enrichment attributes.
    [Fact]
    public async Task Compound_coded_pieces_to_camera_pages_are_matched_to_their_plain_coded_storyboard_row()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var eLAssets = await context.Assets
            .Include(a => a.Attributes)
            .Where(a => a.Code == "E-L")
            .ToListAsync();

        // Three E-L pages in production_plan.md: EP1, EP3, EP5 — normalized to the plain "E-L" code,
        // not left as "E-L / EP1" etc.
        Assert.Equal(3, eLAssets.Count);
        Assert.All(eLAssets, a => Assert.DoesNotContain("/", a.Code));

        // At least one of them was matched against storyboard.html's single "E-L" shot row and
        // enriched with its StoryboardSceneSetup/StoryboardSetupSection attributes. (CaptureNote
        // is deliberately NOT asserted here: Task 13's review found storyboard.html's Setup E table
        // is only 2 columns in the real file, so ParsedShotRow.CaptureNote is genuinely empty for
        // E-L/E-B/E-S, and AddAttribute correctly skips empty values — that's real source data, not
        // a mapping defect.)
        Assert.Contains(eLAssets, a => a.Attributes.Any(attr => attr.Key == "StoryboardSceneSetup" && attr.Value.Length > 0));
        Assert.Contains(eLAssets, a => a.Attributes.Any(attr => attr.Key == "StoryboardSetupSection" && attr.Value.Length > 0));
    }
}
