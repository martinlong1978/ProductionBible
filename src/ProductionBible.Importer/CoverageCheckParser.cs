using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ProductionBible.Importer;

public record CoverageCodeEntry(string Code, bool IsAnimation);

public record CoverageBeatEntry(int EpisodeNumber, string Timecode, List<CoverageCodeEntry> Codes);

public static class CoverageCheckParser
{
    public static List<CoverageBeatEntry> Parse(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<CoverageBeatEntry>();

        var heading = doc.DocumentNode.SelectNodes("//h3")?
            .FirstOrDefault(h => Decode(h.InnerText).Trim() == "Coverage check");
        if (heading is null) return results;

        var tableWrap = heading.SelectSingleNode("following-sibling::div[contains(@class,'tablewrap')][1]");
        var trs = tableWrap?.SelectNodes(".//tbody/tr");
        if (trs is null) return results;

        foreach (var tr in trs)
        {
            var tds = tr.SelectNodes("td");
            if (tds is null || tds.Count < 2) continue;

            var episodeMatch = Regex.Match(Decode(tds[0].InnerText), @"\d+");
            if (!episodeMatch.Success) continue;
            var episodeNumber = int.Parse(episodeMatch.Value);

            var decoded = Decode(tds[1].InnerHtml);
            foreach (var rawSegment in decoded.Split('·'))
            {
                var entry = ParseSegment(episodeNumber, rawSegment.Trim());
                if (entry is not null) results.Add(entry);
            }
        }

        return results;
    }

    private static CoverageBeatEntry? ParseSegment(int episodeNumber, string segment)
    {
        var timecodeMatch = Regex.Match(segment, @"^<code>(?<tc>[^<]+)</code>");
        if (!timecodeMatch.Success) return null;

        var timecode = Decode(timecodeMatch.Groups["tc"].Value).Trim();
        var rest = segment[timecodeMatch.Length..];

        var codes = new List<CoverageCodeEntry>();
        // <code>...</code> (e.g. "keypad.svg") and <em>...</em> ("no capture" annotations) are
        // consumed as whole atomic units here specifically so their inner text never leaks
        // into the plain-text branch below and gets mistaken for a shot code.
        var tokenPattern = @"<code>.*?</code>|<em>.*?</em>|<strong>(?<s>.*?)</strong>|(?<t>[^<]+)";
        foreach (Match token in Regex.Matches(rest, tokenPattern))
        {
            if (token.Groups["s"].Success)
            {
                var code = Decode(token.Groups["s"].Value).Trim();
                if (code.Length > 0) codes.Add(new CoverageCodeEntry(code, IsAnimation: true));
                continue;
            }

            if (!token.Groups["t"].Success) continue; // an <code>/<em> atomic-skip match

            var text = Regex.Replace(token.Groups["t"].Value, @"\([^)]*\)", "");
            foreach (var piece in text.Split(',', '+'))
            {
                var candidate = Decode(piece).Trim();
                foreach (var shotCode in ExpandShotCode(candidate))
                {
                    codes.Add(new CoverageCodeEntry(shotCode, IsAnimation: false));
                }
            }
        }

        return new CoverageBeatEntry(episodeNumber, timecode, codes);
    }

    private static IEnumerable<string> ExpandShotCode(string candidate)
    {
        var rangeMatch = Regex.Match(candidate, @"^(?<letter>[A-F])-(?<nums>\d+(?:/\d+)+)$");
        if (rangeMatch.Success)
        {
            foreach (var num in rangeMatch.Groups["nums"].Value.Split('/'))
            {
                yield return $"{rangeMatch.Groups["letter"].Value}-{num}";
            }
            yield break;
        }

        if (Regex.IsMatch(candidate, @"^[A-F]-[A-Za-z0-9]+$"))
        {
            yield return candidate;
        }
    }

    private static string Decode(string html) => HtmlEntity.DeEntitize(html);
}
