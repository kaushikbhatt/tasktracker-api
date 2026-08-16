using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;

namespace TaskTracker.Controllers;

[ApiController]
[Route("urgency-levels")]
public sealed class UrgencyLevelsController : ControllerBase
{
    private readonly ITaskItemRepository _repo;

    public UrgencyLevelsController(ITaskItemRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UrgencyLevelDto>>> List(CancellationToken ct)
    {
        var items = await _repo.GetUrgencyLevelsAsync(ct);
        var dtos = items.Select(u => new UrgencyLevelDto { Id = u.Id, Name = u.Name, SortOrder = u.SortOrder }).ToList();
        return Ok(dtos);
    }
}