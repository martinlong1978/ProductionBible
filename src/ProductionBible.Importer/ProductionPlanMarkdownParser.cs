using System.Text.RegularExpressions;
using ProductionBible.Importer.Models;

namespace ProductionBible.Importer;

public static class ProductionPlanMarkdownParser
{
    public static List<ParsedShotPage> Parse(string markdown)
    {
        var pages = new List<ParsedShotPage>();
        var chunks = Regex.Split(markdown, @"(?=^### )", RegexOptions.Multiline);

        foreach (var chunk in chunks)
        {
            if (!chunk.TrimStart().StartsWith("### ", StringComparison.Ordinal)) continue;

            // Code is normally a single token ("F-01"), but the seven Phase 6 "pieces to camera"
            // pages reuse a base code across multiple episodes and disambiguate it inline in the
            // heading itself, e.g. "### E-B / EP1 — Bench pieces to camera, Episode 1" — the code
            // is "E-B / EP1", not "E-B" (a bare \S+ would truncate at the space and then fail to
            // find " — " immediately after, dropping these 7 of 65 pages entirely). Capture the
            // code lazily up to the first " — " instead of assuming it is whitespace-free.
            var headingMatch = Regex.Match(chunk, @"^###\s+(?<code>.+?)\s+—\s+(?<title>.+?)\s*$", RegexOptions.Multiline);
            if (!headingMatch.Success) continue;

            var metaMatch = Regex.Match(
                chunk,
                @"<p class=""meta"">Sequence (?<seq>\d+) of \d+ &nbsp;&middot;&nbsp; Phase (?<phaseNum>\d+): (?<phaseName>[^&]+?) &nbsp;&middot;&nbsp; (?<timecodes>.+?)</p>");
            if (!metaMatch.Success) continue;

            var (episodeNumber, timecodes) = ParseEpisodeAndTimecodes(metaMatch.Groups["timecodes"].Value);

            var fields = ParseFieldTable(chunk);

            // Six of the seven Phase 6 "pieces to camera" compound pages (E-B / EP1, E-B / EP2,
            // E-L / EP3, E-B / EP4, E-B / EP5, E-L / EP1) mark their script section with a bold-text
            // heading — "**Script — read through in this order**" — instead of the "#### Script"
            // heading every other page (including the seventh compound page, E-L / EP5) uses.
            // Confirmed directly against the fixture: the bold-heading section still ends at the
            // next "#### " heading (always "#### Additional considerations" immediately after),
            // and its spoken lines are still "> " blockquotes — sub-beats within the section are
            // introduced by their own bold timecode headings (e.g. "**13:30 — Getting it onto my
            // lathe**"), which ParseSection already ignores since only "> " lines are kept. So the
            // same ParseSection logic applies once the alternate heading text is tried as a fallback.
            var scriptText = ParseSection(chunk, "#### Script", ">")
                ?? ParseSection(chunk, "**Script — read through in this order**", ">");

            pages.Add(new ParsedShotPage(
                Code: headingMatch.Groups["code"].Value.Trim(),
                Title: headingMatch.Groups["title"].Value.Trim(),
                SequenceNumber: int.Parse(metaMatch.Groups["seq"].Value),
                PhaseGroup: $"Phase {metaMatch.Groups["phaseNum"].Value}: {metaMatch.Groups["phaseName"].Value.Trim()}",
                EpisodeNumber: episodeNumber,
                Timecodes: timecodes,
                Location: fields.GetValueOrDefault("location"),
                SceneSetup: fields.GetValueOrDefault("setup"),
                AngleAndCamera: fields.GetValueOrDefault("angle & camera"),
                AudioNotes: fields.GetValueOrDefault("audio to capture"),
                TargetLengthRaw: fields.GetValueOrDefault("target length"),
                ScriptText: scriptText,
                AdditionalConsiderations: ParseSection(chunk, "#### Additional considerations", "-")));
        }

        return pages;
    }

    /// <summary>
    /// Extracts the single episode number and its timecodes from the meta line's trailing
    /// "timecodes" segment, e.g. "EP2 00:00 &amp;middot; 22:30".
    ///
    /// KNOWN LIMITATION (documented in the Task 14 brief, confirmed present in the real
    /// production_plan.md fixture): a handful of shot pages are reused across two episodes and
    /// record it in the same segment, using the *same* "&amp;middot;" separator (or, in two cases,
    /// a literal "·" character) that ordinarily separates multiple timecodes within one episode —
    /// e.g. "EP1 16:30 &amp;middot; EP5 21:30" (C-series Setup A/B day) or "EP4 13:00 · EP5 10:00"
    /// (C-08). There is no syntactic way to tell "another timecode in this episode" from "a second
    /// episode" other than checking whether the next token itself starts with "EP&lt;digits&gt;".
    /// This parser takes only the *first* episode's block and stops before the next "EP&lt;digits&gt;"
    /// token, exactly as the brief anticipates ("only the first EP token is captured"). Any
    /// timecodes after a second EP switch are dropped here, not silently misattributed to the
    /// first episode. Task 15 (Beat generation) must not assume every shot's Beats belong to a
    /// single episode without checking against the source document for these pages.
    ///
    /// A second, rarer shape has no "EP&lt;digits&gt;" token at all — e.g. C-11's "All five episodes"
    /// (a recurring pickup shot with no single fixed timecode). For those, EpisodeNumber is 0
    /// (sentinel: "not a single episode") and Timecodes is a single-element list holding the raw
    /// segment text verbatim, so every page still yields at least one non-empty timecode entry
    /// rather than silently producing none.
    /// </summary>
    private static (int EpisodeNumber, List<string> Timecodes) ParseEpisodeAndTimecodes(string timecodesValue)
    {
        var epMatch = Regex.Match(timecodesValue, @"EP(?<ep>\d+)\s+(?<times>.+)$");
        if (!epMatch.Success)
        {
            // No "EPn" token anywhere in this segment (e.g. "All five episodes") — there is no
            // single episode to attach this page to. Keep the raw text as the sole timecode entry
            // so the page still satisfies "at least one timecode" without inventing a fake one.
            var raw = timecodesValue.Trim();
            return (0, raw.Length > 0 ? new List<string> { raw } : new List<string> { timecodesValue });
        }

        var rawTokens = Regex.Split(epMatch.Groups["times"].Value, @"\s*(?:&middot;|·)\s*")
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();

        // Stop at the first token that itself starts a new "EPn" block (see doc comment above) —
        // everything from there on belongs to a different episode and is out of scope for this page.
        var timecodes = new List<string>();
        foreach (var token in rawTokens)
        {
            if (Regex.IsMatch(token, @"^EP\d+\b")) break;
            timecodes.Add(SanitizeTimecode(token));
        }

        // Defensive fallback: should not trigger against the real file (the first token is always
        // a genuine timecode), but never return zero timecodes for a page where an EP token was found.
        if (timecodes.Count == 0) timecodes = rawTokens;

        return (int.Parse(epMatch.Groups["ep"].Value), timecodes);
    }

    /// <summary>
    /// One real page (E-L / EP5, the final shot in the whole plan) has a trailing editorial note
    /// baked into the same timecode segment: "EP5 23:00 — the closing beat of the entire series".
    /// If the token is a bare "MM:SS" (or "H:MM:SS") it is returned unchanged; otherwise, if it
    /// *starts* with a clean timecode followed by descriptive prose, only the leading timecode is
    /// kept, so Task 15 always receives a parseable value rather than a timecode-plus-sentence blob.
    /// </summary>
    private static string SanitizeTimecode(string token)
    {
        var match = Regex.Match(token, @"^(?<clean>\d{1,2}:\d{2}(?::\d{2})?)\b");
        return match.Success ? match.Groups["clean"].Value : token;
    }

    private static Dictionary<string, string> ParseFieldTable(string chunk)
    {
        var fields = new Dictionary<string, string>();
        foreach (Match m in Regex.Matches(
            chunk, @"^\|\s*\*\*(?<key>[^*]+)\*\*\s*\|\s*(?<value>.*?)\s*\|\s*$", RegexOptions.Multiline))
        {
            fields[m.Groups["key"].Value.Trim().ToLowerInvariant()] = m.Groups["value"].Value.Trim();
        }
        return fields;
    }

    private static string? ParseSection(string chunk, string heading, string linePrefix)
    {
        var startIndex = chunk.IndexOf(heading, StringComparison.Ordinal);
        if (startIndex < 0) return null;

        var afterHeading = chunk[(startIndex + heading.Length)..];
        var nextHeadingMatch = Regex.Match(afterHeading, @"^####\s", RegexOptions.Multiline);
        var section = nextHeadingMatch.Success ? afterHeading[..nextHeadingMatch.Index] : afterHeading;

        var lines = section.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith(linePrefix, StringComparison.Ordinal))
            .Select(l => l.TrimStart(linePrefix[0]).Trim())
            .ToList();

        if (lines.Count == 0) return null;
        return linePrefix == ">" ? string.Join(" ", lines) : string.Join("\n", lines.Select(l => $"- {l}"));
    }
}
