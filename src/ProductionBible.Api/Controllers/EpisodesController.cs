using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
public class EpisodesController : ControllerBase
{
    private readonly IEpisodeService _service;

    public EpisodesController(IEpisodeService service)
    {
        _service = service;
    }

    [HttpGet("api/projects/{projectId:int}/episodes")]
    public async Task<ActionResult<IReadOnlyList<EpisodeDto>>> GetByProject(int projectId)
        => Ok(await _service.GetByProjectAsync(projectId));

    [HttpGet("api/episodes/{id:int}")]
    public async Task<ActionResult<EpisodeDto>> GetById(int id)
    {
        var episode = await _service.GetByIdAsync(id);
        return episode is null ? NotFound() : Ok(episode);
    }

    [HttpPost("api/projects/{projectId:int}/episodes")]
    public async Task<ActionResult<EpisodeDto>> Create(int projectId, CreateEpisodeRequest request)
    {
        var created = await _service.CreateAsync(projectId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("api/episodes/{id:int}")]
    public async Task<ActionResult<EpisodeDto>> Update(int id, UpdateEpisodeRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("api/episodes/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
