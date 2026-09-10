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
