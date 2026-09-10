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
