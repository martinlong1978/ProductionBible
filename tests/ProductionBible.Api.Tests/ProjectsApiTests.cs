using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ProductionBible.Application.Dtos;

namespace ProductionBible.Api.Tests;

public class ProjectsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProjectsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_then_get_round_trips_a_project_through_the_real_http_pipeline()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/projects", new CreateProjectRequest("HalfNut ELS", "The lathe series"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

        Assert.NotNull(created);
        var getResponse = await client.GetAsync($"/api/projects/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.Equal("HalfNut ELS", fetched!.Name);
    }

    [Fact]
    public async Task Get_unknown_project_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/projects/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
