namespace ProductionBible.Importer.Models;

public record ParsedShotRow(
    string Code,
    string EpisodeTimecodeRaw,
    string SceneSetup,
    string CaptureNote,
    string SetupSection);
