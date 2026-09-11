using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;

namespace ProductionBible.Importer.Tests;

/// <summary>
/// Runs ImportMapper.ImportAsync against a REAL, throwaway SQLite file database rather than
/// the InMemory provider used by ImportMapperTests. This matters specifically for the Coverage
/// Check application block: it looks up an existing AssetBeat link within a beat by comparing
/// AssetId/foreign keys, but SaveChangesAsync() runs exactly once, at the very end of
/// ImportAsync. With the InMemory provider, Add() assigns a real, permanent key immediately, so
/// an ID-based comparison happens to work there. Against a real relational provider (SQLite,
/// same as production), every Asset/AssetBeat added earlier in the same run still has Id == 0
/// (the CLR default) until SaveChanges actually performs the INSERT — so an ID-based comparison
/// silently compares 0 == 0 for every not-yet-saved entity, corrupting which link is treated as
/// "already there" and which OrderInBeat gets stomped on. Follows the temp-file-plus-
/// ClearAllPools teardown pattern already established in
/// tests/ProductionBible.Api.Tests/TestWebApplicationFactory.cs for the same Windows
/// file-locking reason.
/// </summary>
public class ImportMapperSqliteTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"productionbible-importer-test-{Guid.NewGuid()}.db");

    private ProductionBibleDbContext CreateSqliteContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        var context = new ProductionBibleDbContext(options);
        context.Database.Migrate();
        return context;
    }

    private static string LoadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            var path = _dbPath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Against_a_real_database_every_coverage_check_code_in_a_multi_code_beat_gets_its_own_correct_link()
    {
        await using var context = CreateSqliteContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        // Coverage Check, EP1 "16:30": "G5, F-02, E-B" — three distinct codes in one beat.
        // With the Id-based bug, every not-yet-saved Asset/AssetBeat compares as Id == 0, so
        // FirstOrDefault(ab => ab.AssetId == X.Id) returns the wrong (or same) entry for every
        // code after the first, silently dropping links and stomping OrderInBeat.
        var beat = await context.Beats
            .Include(b => b.AssetBeats).ThenInclude(ab => ab.Asset)
            .Include(b => b.Episode)
            .SingleAsync(b => b.Episode!.Name == "EP1" && b.SourceTimecode == "16:30");

        var orderedCodes = beat.AssetBeats
            .Where(ab => ab.OrderInBeat != null)
            .OrderBy(ab => ab.OrderInBeat)
            .Select(ab => ab.Asset!.Code)
            .ToList();
        Assert.Equal(new List<string> { "g5_defaults", "F-02", "E-B" }, orderedCodes);
    }

    [Fact]
    public async Task Against_a_real_database_the_first_code_in_a_new_coverage_only_beat_keeps_its_own_order_when_a_later_code_cant_link()
    {
        await using var context = CreateSqliteContext();
        var mapper = new ImportMapper(context);

        await mapper.ImportAsync(LoadFixture("storyboard.html"), LoadFixture("production_plan.md"), "HalfNut ELS");

        // Coverage Check, EP1 "09:00": "f1, E-B" — "f1" resolves by prefix to the animation
        // asset f1_notanengineer. This beat has no production_plan.md page at all, so E-B (a
        // shot code) has no pre-existing link here and is correctly never linked
        // (production_plan.md's own timecodes for E-B don't include 09:00). Under the
        // Id-comparison bug, f1_notanengineer's freshly-added (still Id == 0) AssetBeat was
        // wrongly matched as E-B's "existing link" (0 == 0) and had its OrderInBeat stomped from
        // 0 to 1 — silently corrupting the one real link this beat has, with no warning printed
        // for either code.
        var beat = await context.Beats
            .Include(b => b.AssetBeats).ThenInclude(ab => ab.Asset)
            .Include(b => b.Episode)
            .SingleAsync(b => b.Episode!.Name == "EP1" && b.SourceTimecode == "09:00");

        var f1Link = Assert.Single(beat.AssetBeats);
        Assert.Equal("f1_notanengineer", f1Link.Asset!.Code);
        Assert.Equal(0, f1Link.OrderInBeat);
    }
}
