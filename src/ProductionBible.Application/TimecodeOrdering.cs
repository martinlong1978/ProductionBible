using System.Text.RegularExpressions;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application;

public static class TimecodeOrdering
{
    public static bool TryParseSeconds(string timecode, out int seconds)
    {
        var match = Regex.Match(timecode, @"^(?:(?<h>\d+):)?(?<m>\d{1,2}):(?<s>\d{2})$");
        if (!match.Success)
        {
            seconds = 0;
            return false;
        }

        var hours = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value) : 0;
        var minutes = int.Parse(match.Groups["m"].Value);
        var secs = int.Parse(match.Groups["s"].Value);
        seconds = hours * 3600 + minutes * 60 + secs;
        return true;
    }

    /// <summary>
    /// Assigns Ordinal (0-based, sorted by parsed SourceTimecode, unparseable last) and
    /// DurationSeconds (gap to the next beat in that order; 60-second fallback for the last
    /// beat, or whenever the next beat's timecode isn't parseable) to every beat in the list.
    /// Mutates the beats in place. `beats` must already be scoped to a single episode.
    /// </summary>
    public static void AssignOrdinalsAndDurations(IReadOnlyList<Beat> beats)
    {
        var ordered = beats
            .Select(b => new
            {
                Beat = b,
                Parsed = TryParseSeconds(b.SourceTimecode, out var seconds),
                Seconds = seconds,
            })
            .OrderBy(x => x.Parsed ? 0 : 1)
            .ThenBy(x => x.Seconds)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Beat.Ordinal = i;

            var hasNext = i + 1 < ordered.Count;
            var gapIsMeaningful = hasNext && ordered[i].Parsed && ordered[i + 1].Parsed;
            ordered[i].Beat.DurationSeconds = gapIsMeaningful
                ? ordered[i + 1].Seconds - ordered[i].Seconds
                : 60;
        }
    }
}
