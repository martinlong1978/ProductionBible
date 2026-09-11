using System.Net.Http.Json;
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests;

public class BeatReorderSqliteTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BeatReorderSqliteTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reordering_two_beats_through_the_real_http_pipeline_persists_the_new_order()
    {
        var client = _factory.CreateClient();

        var project = await (await client.PostAsJsonAsync(
            "/api/projects", new CreateProjectRequest("HalfNut ELS", null))).Content.ReadFromJsonAsync<ProjectDto>();
        var episode = await (await client.PostAsJsonAsync(
            $"/api/projects/{project!.Id}/episodes", new CreateEpisodeRequest("EP1", 1))).Content.ReadFromJsonAsync<EpisodeDto>();
        var beatA = await (await client.PostAsJsonAsync(
            $"/api/episodes/{episode!.Id}/beats", new CreateBeatRequest("00:00", "A", 0, 60))).Content.ReadFromJsonAsync<BeatDto>();
        var beatB = await (await client.PostAsJsonAsync(
            $"/api/episodes/{episode.Id}/beats", new CreateBeatRequest("01:00", "B", 1, 60))).Content.ReadFromJsonAsync<BeatDto>();

        var reorderResponse = await client.PatchAsJsonAsync(
            $"/api/episodes/{episode.Id}/beats/reorder", new ReorderRequest(new[] { beatB!.Id, beatA!.Id }));
        reorderResponse.EnsureSuccessStatusCode();

        var beats = await (await client.GetAsync($"/api/episodes/{episode.Id}/beats"))
            .Content.ReadFromJsonAsync<List<BeatDto>>();

        Assert.Equal("B", beats![0].Purpose);
        Assert.Equal("A", beats[1].Purpose);
    }
}
