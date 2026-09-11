using System.Text.RegularExpressions;
using ProductionBible.Application.Data;
using ProductionBible.Application.Entities;

namespace ProductionBible.Importer;

public class ImportMapper
{
    private readonly ProductionBibleDbContext _db;
    private readonly Dictionary<int, Episode> _episodesByNumber = new();
    private readonly Dictionary<string, AssetType> _assetTypesByName = new();
    private readonly Dictionary<(int episodeNumber, string timecode), Beat> _beatsByKey = new();
    private readonly Dictionary<(int episodeNumber, string code), Asset> _assetsByEpisodeAndCode = new();

    public ImportMapper(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<Project> ImportAsync(string storyboardHtml, string productionPlanMarkdown, string projectName)
    {
        var (shotRows, animationRows) = StoryboardHtmlParser.Parse(storyboardHtml);
        var shotPages = ProductionPlanMarkdownParser.Parse(productionPlanMarkdown);
        var shotRowsByCode = shotRows.ToDictionary(r => r.Code, r => r);

        var project = new Project { Name = projectName };
        _db.Projects.Add(project);

        var shotType = GetOrCreateAssetType("Shot");
        var pieceToCameraType = GetOrCreateAssetType("PieceToCamera");

        var matchedCodes = new HashSet<string>();
        foreach (var page in shotPages)
        {
            // production_plan.md disambiguates the 7 "pieces to camera" pages that are the same
            // base shot filmed once per episode with a compound code ("E-L / EP1"), while
            // storyboard.html's shot rows use only the plain base code ("E-L") in one summary row.
            // Normalize before both the Asset.Code assignment and the shotRowsByCode lookup, or
            // these 7 pages never match their storyboard.html row and E-L/E-B/E-S spuriously show
            // up as unmatched.
            var normalizedCode = NormalizeCode(page.Code);

            var assetType = normalizedCode.StartsWith("E-", StringComparison.Ordinal) ? pieceToCameraType : shotType;
            var episode = GetOrCreateEpisode(project, page.EpisodeNumber);

            var asset = new Asset
            {
                Episode = episode,
                AssetType = assetType,
                Code = normalizedCode,
                Title = page.Title,
                ScriptText = page.ScriptText,
                Status = "Planned",
                SequenceNumber = page.SequenceNumber,
            };

            AddAttribute(asset, "Location", page.Location);
            AddAttribute(asset, "SceneSetup", page.SceneSetup);
            AddAttribute(asset, "AngleAndCamera", page.AngleAndCamera);
            AddAttribute(asset, "AudioNotes", page.AudioNotes);
            AddAttribute(asset, "TargetLengthRaw", page.TargetLengthRaw);
            AddAttribute(asset, "AdditionalConsiderations", page.AdditionalConsiderations);
            AddAttribute(asset, "PhaseGroup", page.PhaseGroup);

            if (shotRowsByCode.TryGetValue(normalizedCode, out var shotRow))
            {
                matchedCodes.Add(normalizedCode);
                AddAttribute(asset, "CaptureNote", shotRow.CaptureNote);
                AddAttribute(asset, "StoryboardSceneSetup", shotRow.SceneSetup);
                AddAttribute(asset, "StoryboardSetupSection", shotRow.SetupSection);
            }

            _db.Assets.Add(asset);
            _assetsByEpisodeAndCode[(page.EpisodeNumber, normalizedCode)] = asset;

            foreach (var timecode in page.Timecodes)
            {
                var beat = GetOrCreateBeat(project, page.EpisodeNumber, timecode, page.Title);
                _db.AssetBeats.Add(new AssetBeat { Asset = asset, Beat = beat });
            }
        }

        foreach (var unmatchedCode in shotRowsByCode.Keys.Except(matchedCodes))
        {
            Console.WriteLine(
                $"Warning: storyboard.html shot row '{unmatchedCode}' has no matching production_plan.md page; skipped.");
        }

        var animationType = GetOrCreateAssetType("Animation");
        var titleType = GetOrCreateAssetType("Title");

        foreach (var row in animationRows)
        {
            var episodeMatch = Regex.Match(row.Description, @"EP\s*(?<ep>\d+)");
            if (!episodeMatch.Success)
            {
                Console.WriteLine($"Warning: animation row '{row.Code}' has no identifiable episode; skipped.");
                continue;
            }

            var episode = GetOrCreateEpisode(project, int.Parse(episodeMatch.Groups["ep"].Value));
            var isTitle = Regex.IsMatch(row.Code, @"^(t\d+_title|l\d+_)");

            var asset = new Asset
            {
                Episode = episode,
                AssetType = isTitle ? titleType : animationType,
                Code = row.Code,
                Title = row.Code,
                Status = "Planned",
                TargetLengthSeconds = row.DurationSeconds,
                Notes = row.Description,
            };
            AddAttribute(asset, "SourceScriptRef", row.Code);
            _db.Assets.Add(asset);
            var animationEpisodeNumber = int.Parse(episodeMatch.Groups["ep"].Value);
            _assetsByEpisodeAndCode[(animationEpisodeNumber, asset.Code)] = asset;
        }

        foreach (var beatEntry in CoverageCheckParser.Parse(storyboardHtml))
        {
            // Coverage Check entries include pure-graphic beats with no production_plan.md page
            // at all (nothing to film — e.g. EP1 07:20 "The ELS idea, in one sentence"), so this
            // is the only place such a Beat is ever created. GetOrCreateBeat is keyed by
            // (episode, timecode) and checks _beatsByKey first, so a beat already created from a
            // shot page's own timecode is reused here, never duplicated.
            var beat = GetOrCreateBeat(project, beatEntry.EpisodeNumber, beatEntry.Timecode, purpose: "");

            for (var i = 0; i < beatEntry.Codes.Count; i++)
            {
                var codeEntry = beatEntry.Codes[i];

                if (!codeEntry.IsAnimation)
                {
                    if (!_assetsByEpisodeAndCode.TryGetValue((beatEntry.EpisodeNumber, codeEntry.Code), out var shotAsset))
                    {
                        Console.WriteLine(
                            $"Warning: Coverage check shot code '{codeEntry.Code}' in EP{beatEntry.EpisodeNumber} " +
                            $"{beatEntry.Timecode} has no matching asset; skipped.");
                        continue;
                    }

                    var existingLink = beat.AssetBeats.FirstOrDefault(ab => ab.Asset == shotAsset);
                    if (existingLink is null)
                    {
                        Console.WriteLine(
                            $"Warning: Coverage check pairs '{codeEntry.Code}' with EP{beatEntry.EpisodeNumber} " +
                            $"{beatEntry.Timecode}, but production_plan.md's own timecodes for that shot didn't; skipped.");
                        continue;
                    }

                    existingLink.OrderInBeat = i;
                }
                else
                {
                    var animationAsset = ResolveAnimationCode(beatEntry.EpisodeNumber, codeEntry.Code);
                    if (animationAsset is null)
                    {
                        Console.WriteLine(
                            $"Warning: Coverage check animation code '{codeEntry.Code}' in EP{beatEntry.EpisodeNumber} " +
                            $"{beatEntry.Timecode} has no matching asset (not yet built?); skipped.");
                        continue;
                    }

                    var existingLink = beat.AssetBeats.FirstOrDefault(ab => ab.Asset == animationAsset);
                    if (existingLink is not null)
                    {
                        existingLink.OrderInBeat = i;
                    }
                    else
                    {
                        _db.AssetBeats.Add(new AssetBeat { Asset = animationAsset, Beat = beat, OrderInBeat = i });
                    }
                }
            }
        }

        await _db.SaveChangesAsync();
        return project;
    }

    /// <summary>
    /// Strips a trailing " / EPn" disambiguator off a production_plan.md page code, e.g.
    /// "E-L / EP1" -> "E-L". Plain codes (the other 58 of 65 pages) pass through unchanged.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        var match = Regex.Match(code, @"^(?<base>.+?)\s*/\s*EP\d+$");
        return match.Success ? match.Groups["base"].Value.Trim() : code;
    }

    private Episode GetOrCreateEpisode(Project project, int number)
    {
        if (_episodesByNumber.TryGetValue(number, out var existing)) return existing;
        var episode = new Episode { Project = project, Name = $"EP{number}", OrderIndex = number };
        _episodesByNumber[number] = episode;
        _db.Episodes.Add(episode);
        return episode;
    }

    private AssetType GetOrCreateAssetType(string name)
    {
        if (_assetTypesByName.TryGetValue(name, out var existing)) return existing;
        var type = new AssetType { Name = name };
        _assetTypesByName[name] = type;
        _db.AssetTypes.Add(type);
        return type;
    }

    private Beat GetOrCreateBeat(Project project, int episodeNumber, string timecode, string purpose)
    {
        var key = (episodeNumber, timecode);
        if (_beatsByKey.TryGetValue(key, out var existing)) return existing;
        var episode = GetOrCreateEpisode(project, episodeNumber);
        var beat = new Beat { Episode = episode, Timecode = timecode, Purpose = purpose };
        _beatsByKey[key] = beat;
        _db.Beats.Add(beat);
        return beat;
    }

    // Unlike shot/PTC codes (genuinely per-episode, e.g. "E-L" is a different Asset in every
    // episode it's filmed in), an animation Asset is built once and can be *reused* across
    // episodes (G1 is "EP1 07:20, reused EP2"; g5_defaults is built as EP2's asset but its
    // Coverage Check reference at EP1 16:30 is a reuse of that same asset). So an exact/prefix
    // match in the beat's own episode wins if one exists, but a match from any other episode is
    // still a valid resolution — falling back to it, rather than reporting "not yet built",
    // is what makes reuse across episodes actually resolve.
    private Asset? ResolveAnimationCode(int episodeNumber, string code)
    {
        Asset? crossEpisodeMatch = null;
        foreach (var ((ep, assetCode), asset) in _assetsByEpisodeAndCode)
        {
            var isMatch = string.Equals(assetCode, code, StringComparison.OrdinalIgnoreCase)
                || string.Equals(assetCode.Split('_')[0], code, StringComparison.OrdinalIgnoreCase);
            if (!isMatch) continue;
            if (ep == episodeNumber) return asset;
            crossEpisodeMatch ??= asset;
        }
        return crossEpisodeMatch;
    }

    private static void AddAttribute(Asset asset, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        asset.Attributes.Add(new AssetAttribute { Asset = asset, Key = key, Value = value });
    }
}
