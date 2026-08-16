using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;

namespace TaskTracker.Controllers;

[ApiController]
[Route("task-items")]
public sealed class TaskItemsController : ControllerBase
{
    private readonly ITaskItemService _service;

    public TaskItemsController(ITaskItemService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskItemDto>>> List([FromQuery] TaskItemQuery query, CancellationToken ct)
    {
        var result = await _service.QueryAsync(query, ct);
        return Ok(result);
        }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> Create([FromBody] CreateTaskItemDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> Update(Guid id, [FromBody] UpdateTaskItemDto dto, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> Patch(Guid id, [FromBody] PatchTaskItemDto dto, CancellationToken ct)
    {
        var result = await _service.PatchAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<TaskItemDto>> Restore(Guid id, CancellationToken ct)
    {
        var result = await _service.RestoreAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/stage")]
    public async Task<ActionResult<TaskItemDto>> ChangeStage(Guid id, [FromBody] ChangeStageDto dto, CancellationToken ct)
    {
        var result = await _service.ChangeStageAsync(id, dto.TargetStage, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<ActionResult<TaskItemDto>> Reopen(Guid id, CancellationToken ct)
    {
        var result = await _service.ReopenAsync(id, ct);
        return Ok(result);
    }
}