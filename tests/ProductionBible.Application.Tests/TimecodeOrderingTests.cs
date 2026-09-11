using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Tests;

public class TimecodeOrderingTests
{
    [Theory]
    [InlineData("00:00", 0)]
    [InlineData("04:00", 240)]
    [InlineData("22:30", 1350)]
    [InlineData("4:00", 240)]
    [InlineData("01:04:00", 3840)]
    public void TryParseSeconds_parses_valid_timecodes(string timecode, int expectedSeconds)
    {
        var result = TimecodeOrdering.TryParseSeconds(timecode, out var seconds);

        Assert.True(result);
        Assert.Equal(expectedSeconds, seconds);
    }

    [Fact]
    public void TryParseSeconds_returns_false_for_raw_prose()
    {
        var result = TimecodeOrdering.TryParseSeconds(
            "Reused in EP1 20:30 · EP3 12:00 · before the first ENABLE in EP4 and EP5",
            out var seconds);

        Assert.False(result);
        Assert.Equal(0, seconds);
    }
}

public class TimecodeOrdering_AssignOrdinalsAndDurations_Tests
{
    [Fact]
    public void Assigns_Ordinal_and_gap_based_DurationSeconds_in_chronological_order()
    {
        var beatC = new Beat { SourceTimecode = "20:00", Purpose = "Third" };
        var beatA = new Beat { SourceTimecode = "00:00", Purpose = "First" };
        var beatB = new Beat { SourceTimecode = "00:40", Purpose = "Second" };
        var beats = new List<Beat> { beatC, beatA, beatB };

        TimecodeOrdering.AssignOrdinalsAndDurations(beats);

        Assert.Equal(0, beatA.Ordinal);
        Assert.Equal(40, beatA.DurationSeconds);
        Assert.Equal(1, beatB.Ordinal);
        Assert.Equal(1160, beatB.DurationSeconds); // 00:40 -> 20:00 is 1160 seconds
        Assert.Equal(2, beatC.Ordinal);
        Assert.Equal(60, beatC.DurationSeconds); // last beat, no next to gap against
    }

    [Fact]
    public void Sorts_an_unparseable_timecode_last_and_falls_back_to_60_seconds_for_its_neighbor()
    {
        var unscheduled = new Beat { SourceTimecode = "Reused in EP1 20:30 · EP3 12:00", Purpose = "Unscheduled" };
        var scheduled = new Beat { SourceTimecode = "00:00", Purpose = "Cold open" };
        var beats = new List<Beat> { unscheduled, scheduled };

        TimecodeOrdering.AssignOrdinalsAndDurations(beats);

        Assert.Equal(0, scheduled.Ordinal);
        // scheduled's "next" beat (unscheduled) can't be gapped against — 60s fallback, not a
        // huge or negative number from treating the unparseable text as seconds == 0.
        Assert.Equal(60, scheduled.DurationSeconds);
        Assert.Equal(1, unscheduled.Ordinal);
        Assert.Equal(60, unscheduled.DurationSeconds);
    }

    [Fact]
    public void A_single_beat_gets_Ordinal_zero_and_the_60_second_fallback()
    {
        var onlyBeat = new Beat { SourceTimecode = "16:30", Purpose = "Only beat in this episode" };
        var beats = new List<Beat> { onlyBeat };

        TimecodeOrdering.AssignOrdinalsAndDurations(beats);

        Assert.Equal(0, onlyBeat.Ordinal);
        Assert.Equal(60, onlyBeat.DurationSeconds);
    }
}
