using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Services.RecoveryActions;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/recovery-actions")]
public class RecoveryActionsController(IRecoveryActionService recoveryActionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RecoveryActionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecoveryActionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var actions = await recoveryActionService.GetAllAsync(cancellationToken);
        return Ok(actions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RecoveryActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecoveryActionDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var action = await recoveryActionService.GetByIdAsync(id, cancellationToken);
        if (action is null)
            return NotFound(new { message = $"Recovery action me id {id} nuk u gjet." });

        return Ok(action);
    }

    [HttpGet("by-incident/{incidentId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<RecoveryActionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecoveryActionDto>>> GetByIncidentId(
        int incidentId,
        CancellationToken cancellationToken)
    {
        var actions = await recoveryActionService.GetByIncidentIdAsync(incidentId, cancellationToken);
        return Ok(actions);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RecoveryActionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecoveryActionDto>> Create(
        [FromBody] CreateRecoveryActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var action = await recoveryActionService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = action.Id }, action);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(RecoveryActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecoveryActionDto>> UpdateStatus(
        int id,
        [FromBody] UpdateRecoveryActionStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var action = await recoveryActionService.UpdateStatusAsync(id, request.ActionStatus, cancellationToken);
            if (action is null)
                return NotFound(new { message = $"Recovery action me id {id} nuk u gjet." });

            return Ok(action);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/execute")]
    [ProducesResponseType(typeof(RecoveryActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecoveryActionDto>> Execute(int id, CancellationToken cancellationToken)
    {
        var action = await recoveryActionService.ExecuteAsync(id, cancellationToken);
        if (action is null)
            return NotFound(new { message = $"Recovery action me id {id} nuk u gjet." });

        return Ok(action);
    }
}
