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
        var timecodes = f01.AssetBeats.Select(ab => ab.Beat!.SourceTimecode).OrderBy(t => t).ToList();
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
    public async Task Animation_rows_become_assets_with_no_sequence_number_but_are_linked_to_their_covering_beats()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var g1 = await context.Assets
            .Include(a => a.AssetBeats).ThenInclude(ab => ab.Beat)
            .Include(a => a.AssetType)
            .SingleAsync(a => a.Code == "g1_gears");
        Assert.Equal("Animation", g1.AssetType!.Name);
        Assert.Null(g1.SequenceNumber);

        // Coverage Check table, EP1: "02:00 B-05 + G1" and "07:20 G1" — g1_gears covers both.
        var timecodes = g1.AssetBeats.Select(ab => ab.Beat!.SourceTimecode).OrderBy(t => t).ToList();
        Assert.Equal(new List<string> { "02:00", "07:20" }, timecodes);
    }

    [Fact]
    public async Task Coverage_check_sets_OrderInBeat_for_both_shot_and_animation_codes_in_the_same_beat()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        // Coverage Check, EP1 "16:30": "G5, F-02, E-B" — G5 (animation) is listed FIRST.
        var beat = await context.Beats
            .Include(b => b.AssetBeats).ThenInclude(ab => ab.Asset)
            .Include(b => b.Episode)
            .SingleAsync(b => b.Episode!.Name == "EP1" && b.SourceTimecode == "16:30");

        var orderedCodes = beat.AssetBeats
            .Where(ab => ab.OrderInBeat != null)
            .OrderBy(ab => ab.OrderInBeat)
            .Select(ab => ab.Asset!.Code)
            .ToList();
        Assert.Equal(new List<string> { "g5_defaults", "F-02", "E-B" }, orderedCodes);
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

    [Fact]
    public async Task Computes_Ordinal_and_DurationSeconds_for_EP1_beats_in_parsed_timecode_order()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var ep1Beats = await context.Beats
            .Include(b => b.Episode)
            .Where(b => b.Episode!.Name == "EP1")
            .OrderBy(b => b.Ordinal)
            .ToListAsync();

        // EP1's Coverage Check table gives these timecodes in order: 00:00, 00:38, 00:50,
        // 02:00, 04:00, 05:40, 07:20, 09:00, 11:00, 13:30, 16:30, 18:30, 20:30, 21:30, 22:20.
        var orderedSourceTimecodes = ep1Beats.Select(b => b.SourceTimecode).ToList();
        Assert.Equal(new List<string>
        {
            "00:00", "00:38", "00:50", "02:00", "04:00", "05:40", "07:20", "09:00",
            "11:00", "13:30", "16:30", "18:30", "20:30", "21:30", "22:20",
        }, orderedSourceTimecodes);

        // Ordinal is 0-based positional.
        Assert.Equal(Enumerable.Range(0, ep1Beats.Count).ToList(), ep1Beats.Select(b => b.Ordinal).ToList());

        // 00:00 -> 00:38 is a 38-second gap.
        Assert.Equal(38, ep1Beats[0].DurationSeconds);
        // 21:30 -> 22:20 is a 50-second gap.
        var beatAt2130 = ep1Beats.Single(b => b.SourceTimecode == "21:30");
        Assert.Equal(50, beatAt2130.DurationSeconds);
        // The last beat in the episode (22:20) falls back to 60 seconds — no next beat to gap against.
        var lastBeat = ep1Beats.Last();
        Assert.Equal("22:20", lastBeat.SourceTimecode);
        Assert.Equal(60, lastBeat.DurationSeconds);
    }

    [Fact]
    public async Task Shot_pages_get_a_real_Phase_row_instead_of_a_PhaseGroup_attribute()
    {
        await using var context = CreateInMemoryContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        var a01 = await context.Assets
            .Include(a => a.Attributes)
            .Include(a => a.Phase)
            .SingleAsync(a => a.Code == "A-01");

        Assert.NotNull(a01.Phase);
        Assert.DoesNotContain(a01.Attributes, attr => attr.Key == "PhaseGroup");

        // Two production_plan.md pages that share a phase (both "Setup A" shots, per the
        // fixture) resolve to the SAME Phase row, not two separate rows with the same name.
        var a02 = await context.Assets.Include(a => a.Phase).SingleAsync(a => a.Code == "A-02");
        Assert.Equal(a01.PhaseId, a02.PhaseId);

        var phaseCount = await context.Phases.CountAsync();
        Assert.True(phaseCount > 0);
    }
}
