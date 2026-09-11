using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _service;

    public AssetsController(IAssetService service)
    {
        _service = service;
    }

    [HttpGet("api/episodes/{episodeId:int}/assets")]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetByEpisode(int episodeId)
        => Ok(await _service.GetByEpisodeAsync(episodeId));

    [HttpGet("api/assets/{id:int}")]
    public async Task<ActionResult<AssetDto>> GetById(int id)
    {
        var asset = await _service.GetByIdAsync(id);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpPost("api/episodes/{episodeId:int}/assets")]
    public async Task<ActionResult<AssetDto>> Create(int episodeId, CreateAssetRequest request)
    {
        var created = await _service.CreateAsync(episodeId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("api/assets/{id:int}")]
    public async Task<ActionResult<AssetDto>> Update(int id, UpdateAssetRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("api/assets/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPatch("api/phases/{phaseId:int}/assets/reorder")]
    public async Task<IActionResult> ReorderWithinPhase(int phaseId, ReorderRequest request)
    {
        var succeeded = await _service.ReorderWithinPhaseAsync(phaseId, request.OrderedIds);
        return succeeded ? NoContent() : BadRequest();
    }

    [HttpPatch("api/beats/{beatId:int}/asset-beats/reorder")]
    public async Task<IActionResult> ReorderWithinBeat(int beatId, ReorderRequest request)
    {
        var succeeded = await _service.ReorderWithinBeatAsync(beatId, request.OrderedIds);
        return succeeded ? NoContent() : BadRequest();
    }
}
