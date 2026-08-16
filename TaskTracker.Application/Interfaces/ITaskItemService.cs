using System;
using System.Collections.Generic;
using System.Text;
using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Interfaces;

/// <summary>
/// The business-rule surface for TaskItem. Every method here corresponds to
/// one endpoint the Api layer will expose.
/// </summary>
public interface ITaskItemService
{
	Task<TaskItemDto> GetByIdAsync(Guid id, CancellationToken ct = default);

	Task<PagedResult<TaskItemDto>> QueryAsync(TaskItemQuery query, CancellationToken ct = default);

	Task<TaskItemDto> CreateAsync(CreateTaskItemDto dto, CancellationToken ct = default);

	Task<TaskItemDto> UpdateAsync(Guid id, UpdateTaskItemDto dto, CancellationToken ct = default);

	Task<TaskItemDto> PatchAsync(Guid id, PatchTaskItemDto dto, CancellationToken ct = default);

	Task DeleteAsync(Guid id, CancellationToken ct = default);

	Task<TaskItemDto> RestoreAsync(Guid id, CancellationToken ct = default);

	/// <summary>Idempotent: calling with the same target stage twice is a no-op
	/// on the second call. </summary>
	Task<TaskItemDto> ChangeStageAsync(Guid id, string targetStage, CancellationToken ct = default);

	/// <summary>The one deliberate way to move a Finished item backward.</summary>
	Task<TaskItemDto> ReopenAsync(Guid id, CancellationToken ct = default);
}