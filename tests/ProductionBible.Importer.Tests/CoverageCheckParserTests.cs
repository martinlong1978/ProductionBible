namespace ProductionBible.Importer.Tests;

public class CoverageCheckParserTests
{
    private static string LoadFixture() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "storyboard.html"));

    [Fact]
    public void Parses_all_five_episodes()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        var episodeNumbers = entries.Select(e => e.EpisodeNumber).Distinct().OrderBy(n => n).ToList();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, episodeNumbers);
    }

    [Fact]
    public void Parses_a_simple_comma_separated_beat()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        var beat = Assert.Single(entries, e => e.EpisodeNumber == 1 && e.Timecode == "04:00");
        Assert.Equal(
            new[] { ("B-02", false), ("B-06", false) },
            beat.Codes.Select(c => (c.Code, c.IsAnimation)));
    }

    [Fact]
    public void Expands_a_shared_prefix_slash_list()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        var beat = Assert.Single(entries, e => e.EpisodeNumber == 1 && e.Timecode == "00:00");
        Assert.Equal(
            new[] { ("A-01", false), ("A-02", false), ("A-03", false), ("B-01", false) },
            beat.Codes.Select(c => (c.Code, c.IsAnimation)));
    }

    [Fact]
    public void Preserves_left_to_right_order_when_an_animation_code_appears_before_shot_codes()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        // EP1 16:30: "G5, F-02, E-B" — the animation code is listed FIRST in the source text.
        // A parser that collected shot codes and animation codes into separate lists and
        // concatenated them afterward would get this wrong.
        var beat = Assert.Single(entries, e => e.EpisodeNumber == 1 && e.Timecode == "16:30");
        Assert.Equal(
            new[] { ("G5", true), ("F-02", false), ("E-B", false) },
            beat.Codes.Select(c => (c.Code, c.IsAnimation)));
    }

    [Fact]
    public void Extracts_a_code_embedded_in_a_plus_joined_pair_with_an_animation_code()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        var beat = Assert.Single(entries, e => e.EpisodeNumber == 1 && e.Timecode == "02:00");
        Assert.Equal(
            new[] { ("B-05", false), ("G1", true) },
            beat.Codes.Select(c => (c.Code, c.IsAnimation)));
    }

    [Fact]
    public void Discards_free_prose_but_keeps_a_real_code_embedded_in_it()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        // EP1 18:30: "montage from A/B/C + B-18" — only B-18 is a real code.
        var beat = Assert.Single(entries, e => e.EpisodeNumber == 1 && e.Timecode == "18:30");
        Assert.Equal(new[] { ("B-18", false) }, beat.Codes.Select(c => (c.Code, c.IsAnimation)));
    }

    [Fact]
    public void Discards_a_parenthetical_derivation_note_but_keeps_the_real_code()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        // EP2 00:00: "C-13 (from F-01)" — only C-13 is a covering code here.
        var beat = Assert.Single(entries, e => e.EpisodeNumber == 2 && e.Timecode == "00:00");
        Assert.Equal(new[] { ("C-13", false) }, beat.Codes.Select(c => (c.Code, c.IsAnimation)));
    }

    [Fact]
    public void Ignores_a_no_capture_annotation_and_still_keeps_the_animation_code()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        // EP2 13:00: "G3 — no capture" — the "no capture" text is not a semantic signal for
        // this parser; it just isn't a code, so it's absent from the result either way.
        var beat = Assert.Single(entries, e => e.EpisodeNumber == 2 && e.Timecode == "13:00");
        Assert.Equal(new[] { ("G3", true) }, beat.Codes.Select(c => (c.Code, c.IsAnimation)));
    }

    [Fact]
    public void Produces_an_empty_code_list_for_a_beat_with_no_extractable_code_at_all()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        // EP2 19:00: "trace plot — no capture" — no code of any kind.
        var beat = Assert.Single(entries, e => e.EpisodeNumber == 2 && e.Timecode == "19:00");
        Assert.Empty(beat.Codes);
    }

    [Fact]
    public void Ignores_an_unrelated_code_tagged_filename_reference()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        // EP3 02:00: "<code>keypad.svg</code>, E-L" — keypad.svg is a firmware-repo docs file,
        // not a known asset code, and must not be extracted as one; E-L still should be.
        var beat = Assert.Single(entries, e => e.EpisodeNumber == 3 && e.Timecode == "02:00");
        Assert.Equal(new[] { ("E-L", false) }, beat.Codes.Select(c => (c.Code, c.IsAnimation)));
    }

    [Fact]
    public void Parses_a_lowercase_letter_suffixed_animation_code()
    {
        var entries = CoverageCheckParser.Parse(LoadFixture());

        var beat = Assert.Single(entries, e => e.EpisodeNumber == 3 && e.Timecode == "22:30");
        Assert.Equal(
            new[] { ("A-11", false), ("A-12", false), ("g2d", true) },
            beat.Codes.Select(c => (c.Code, c.IsAnimation)));
    }
}
