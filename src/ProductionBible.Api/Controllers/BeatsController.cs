using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
public class BeatsController : ControllerBase
{
    private readonly IBeatService _service;

    public BeatsController(IBeatService service)
    {
        _service = service;
    }

    [HttpGet("api/episodes/{episodeId:int}/beats")]
    public async Task<ActionResult<IReadOnlyList<BeatDto>>> GetByEpisode(int episodeId)
        => Ok(await _service.GetByEpisodeAsync(episodeId));

    [HttpGet("api/beats/{id:int}")]
    public async Task<ActionResult<BeatDto>> GetById(int id)
    {
        var beat = await _service.GetByIdAsync(id);
        return beat is null ? NotFound() : Ok(beat);
    }

    [HttpPost("api/episodes/{episodeId:int}/beats")]
    public async Task<ActionResult<BeatDto>> Create(int episodeId, CreateBeatRequest request)
    {
        var created = await _service.CreateAsync(episodeId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("api/beats/{id:int}")]
    public async Task<ActionResult<BeatDto>> Update(int id, UpdateBeatRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("api/beats/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
