using System.Text.RegularExpressions;

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
}
