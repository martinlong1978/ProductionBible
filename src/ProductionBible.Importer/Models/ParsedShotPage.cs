namespace ProductionBible.Importer.Models;

public record ParsedShotPage(
    string Code,
    string Title,
    int SequenceNumber,
    string PhaseGroup,
    int EpisodeNumber,
    List<string> Timecodes,
    string? Location,
    string? SceneSetup,
    string? AngleAndCamera,
    string? AudioNotes,
    string? TargetLengthRaw,
    string? ScriptText,
    string? AdditionalConsiderations);
