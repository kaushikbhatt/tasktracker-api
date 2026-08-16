using System;
using System.Collections.Generic;
using System.Text;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Exceptions;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Services;

public sealed class TaskItemService : ITaskItemService
{
	private readonly ITaskItemRepository _repository;

	public TaskItemService(ITaskItemRepository repository)
	{
		_repository = repository;
	}

	public async Task<TaskItemDto> CreateAsync(CreateTaskItemDto dto, CancellationToken ct = default)
	{
		// unique among ACTIVE items only. The database's filtered
		// index is the real enforcement under concurrency; this check exists
		// to return a fast, friendly 409 in the common non-concurrent case.
		if (await _repository.ExistsWithActiveTitleAsync(dto.Title, excludingId: null, ct))
		{
			throw new TaskItemConflictException(
				"DUPLICATE_ACTIVE_TITLE",
				$"An active TaskItem with the title '{dto.Title}' already exists.");
		}

		if (!await _repository.UrgencyLevelExistsAsync(dto.UrgencyLevelId, ct))
		{
			throw new AppValidationException(nameof(dto.UrgencyLevelId), "Urgency level does not exist.");
		}

		var now = DateTime.UtcNow;
		var entity = new TaskItem
		{
			Id = Guid.NewGuid(),
			Title = dto.Title,
			Notes = dto.Notes,
			Stage = TaskStage.Started,
			UrgencyLevelId = dto.UrgencyLevelId,
			Deadline = dto.Deadline,
			CreatedAtUtc = now,
			UpdatedAtUtc = now,
			IsDeleted = false
		};

		await _repository.AddAsync(entity, ct);
		await _repository.SaveChangesAsync(ct);

		return ToDto(entity, urgencyLevelName: null); // urgency name filled by repo in real query path
	}

	// Remaining ITaskItemService members implemented in following commits:
	// GetByIdAsync, sQueryAsync, UpdateAsync, PatchAsync, DeleteAsync,
	// RestoreAsync, ChangeStageAsync, ReopenAsync.
	public Task<TaskItemDto> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
	public Task<PagedResult<TaskItemDto>> QueryAsync(TaskItemQuery query, CancellationToken ct = default) => throw new NotImplementedException();
	public Task<TaskItemDto> UpdateAsync(Guid id, UpdateTaskItemDto dto, CancellationToken ct = default) => throw new NotImplementedException();
	public Task<TaskItemDto> PatchAsync(Guid id, PatchTaskItemDto dto, CancellationToken ct = default) => throw new NotImplementedException();
	public Task DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
	public Task<TaskItemDto> RestoreAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
	public Task<TaskItemDto> ChangeStageAsync(Guid id, string targetStage, CancellationToken ct = default) => throw new NotImplementedException();
	public Task<TaskItemDto> ReopenAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();

	private static TaskItemDto ToDto(TaskItem entity, string? urgencyLevelName) => new()
	{
		Id = entity.Id,
		Title = entity.Title,
		Notes = entity.Notes,
		Stage = entity.Stage.ToString(),
		UrgencyLevelId = entity.UrgencyLevelId,
		UrgencyLevelName = urgencyLevelName ?? string.Empty,
		Deadline = entity.Deadline,
		CreatedAtUtc = entity.CreatedAtUtc,
		UpdatedAtUtc = entity.UpdatedAtUtc
	};
}