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
