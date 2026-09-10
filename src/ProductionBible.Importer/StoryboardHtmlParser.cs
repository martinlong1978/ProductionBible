using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ProductionBible.Importer.Models;

namespace ProductionBible.Importer;

public static class StoryboardHtmlParser
{
    public static (List<ParsedShotRow> ShotRows, List<ParsedAnimationRow> AnimationRows) Parse(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return (ParseShotRows(doc), ParseAnimationRows(doc));
    }

    private static List<ParsedShotRow> ParseShotRows(HtmlDocument doc)
    {
        var rows = new List<ParsedShotRow>();
        var setupHeadings = doc.DocumentNode.SelectNodes("//h3")?
            .Where(h => Decode(h.InnerText).TrimStart().StartsWith("Setup ", StringComparison.Ordinal))
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var heading in setupHeadings)
        {
            var setupName = Decode(heading.InnerText).Trim();
            var tableWrap = heading.SelectSingleNode("following-sibling::div[contains(@class,'tablewrap')][1]");
            var trs = tableWrap?.SelectNodes(".//tbody/tr");
            if (trs is null) continue;

            foreach (var tr in trs)
            {
                var tds = tr.SelectNodes("td");
                // Most setup tables are 3 columns (Shot | Scene setup | Camera & capture),
                // confirmed for Setup A/B/C/F. Setup D ("Screen capture") and Setup E
                // ("Pieces to camera") in the real file are only 2 columns (Shot | note).
                // Accept both shapes rather than silently dropping the 2-column setups.
                if (tds is null || tds.Count < 2) continue;

                var strong = tds[0].SelectSingleNode(".//strong");
                if (strong is null) continue;
                var span = tds[0].SelectSingleNode(".//span[contains(@class,'small')]");

                var sceneSetup = Decode(tds[1].InnerText).Trim();
                var captureNote = tds.Count >= 3 ? Decode(tds[2].InnerText).Trim() : "";

                rows.Add(new ParsedShotRow(
                    Code: Decode(strong.InnerText).Trim(),
                    EpisodeTimecodeRaw: span is null ? "" : Decode(span.InnerText).Trim(),
                    SceneSetup: sceneSetup,
                    CaptureNote: captureNote,
                    SetupSection: setupName));
            }
        }

        return rows;
    }

    private static List<ParsedAnimationRow> ParseAnimationRows(HtmlDocument doc)
    {
        var results = new List<ParsedAnimationRow>();
        var candidateRows = doc.DocumentNode
            .SelectNodes("//table/tbody/tr[td[1]/code and td[2][contains(@class,'num-col')]]")
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var tr in candidateRows)
        {
            var tds = tr.SelectNodes("td");
            // The loose XPath above also catches incidental 2-column rows elsewhere in the
            // file that happen to carry a <code> snippet in td[1] and a num-col td[2] (e.g. the
            // EP5 "worked example" table's "(attachFullQuad)" row) — genuine animation/title
            // rows are always 3 columns (File | Length | Where it goes), so require that shape.
            if (tds is null || tds.Count < 3) continue;

            var codeCellText = Decode(tds[0].InnerText).Trim();
            var durationText = Decode(tds[1].InnerText).Trim();
            var description = Decode(tds[2].InnerText).Trim();
            var durationSeconds = ParseDurationSeconds(durationText);

            foreach (var code in ExpandCodeRange(codeCellText))
            {
                results.Add(new ParsedAnimationRow(code, durationSeconds, description));
            }
        }

        return results;
    }

    private static string Decode(string html) => HtmlEntity.DeEntitize(html);

    private static int? ParseDurationSeconds(string text)
    {
        var match = Regex.Match(text, @"([\d.]+)\s*s");
        return match.Success && double.TryParse(match.Groups[1].Value, out var seconds)
            ? (int)Math.Round(seconds)
            : null;
    }

    private static IEnumerable<string> ExpandCodeRange(string codeCellText)
    {
        var match = Regex.Match(
            codeCellText,
            @"^(?<pre>[a-zA-Z]+)(?<from>\d+)(?<suf>_[a-zA-Z]+)\s*(?:…|\.\.\.)\s*[a-zA-Z]+(?<to>\d+)_[a-zA-Z]+$");
        if (!match.Success)
        {
            yield return codeCellText;
            yield break;
        }

        var from = int.Parse(match.Groups["from"].Value);
        var to = int.Parse(match.Groups["to"].Value);
        for (var i = from; i <= to; i++)
        {
            yield return $"{match.Groups["pre"].Value}{i}{match.Groups["suf"].Value}";
        }
    }
}
