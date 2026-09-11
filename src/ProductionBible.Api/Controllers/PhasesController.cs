using Microsoft.AspNetCore.Mvc;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Api.Controllers;

[ApiController]
public class PhasesController : ControllerBase
{
    private readonly IPhaseService _service;

    public PhasesController(IPhaseService service)
    {
        _service = service;
    }

    [HttpGet("api/projects/{projectId:int}/phases")]
    public async Task<ActionResult<IReadOnlyList<PhaseDto>>> GetByProject(int projectId)
        => Ok(await _service.GetByProjectAsync(projectId));

    [HttpGet("api/phases/{id:int}")]
    public async Task<ActionResult<PhaseDto>> GetById(int id)
    {
        var phase = await _service.GetByIdAsync(id);
        return phase is null ? NotFound() : Ok(phase);
    }

    [HttpPost("api/projects/{projectId:int}/phases")]
    public async Task<ActionResult<PhaseDto>> Create(int projectId, CreatePhaseRequest request)
    {
        var created = await _service.CreateAsync(projectId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("api/phases/{id:int}")]
    public async Task<ActionResult<PhaseDto>> Update(int id, UpdatePhaseRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("api/phases/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPatch("api/projects/{projectId:int}/phases/reorder")]
    public async Task<IActionResult> Reorder(int projectId, ReorderRequest request)
    {
        var succeeded = await _service.ReorderAsync(projectId, request.OrderedIds);
        return succeeded ? NoContent() : BadRequest();
    }
}
