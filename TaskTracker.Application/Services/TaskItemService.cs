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
	public async Task<TaskItemDto> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		var entity = await _repository.GetByIdAsync(id, includeDeleted: false, ct)
			?? throw new TaskItemNotFoundException(id);
		return ToDto(entity, urgencyLevelName: null);
	}

	public async Task<PagedResult<TaskItemDto>> QueryAsync(TaskItemQuery query, CancellationToken ct = default)
	{
		// Validation on paging inputs -- bad input fails clearly (rule 5.5),
		// it doesn't silently clamp to some default.
		if (query.Page < 1)
		{
			throw new AppValidationException(nameof(query.Page), "Page must be 1 or greater.");
		}
		if (query.PageSize < 1 || query.PageSize > 200)
		{
			throw new AppValidationException(nameof(query.PageSize), "PageSize must be between 1 and 200.");
		}

		// Filtering, sorting, and the total-match count all happen inside
		// the repository, against IQueryable -- never by loading all rows
		// into memory here. See DECISIONS.md, list-endpoint performance.
		var (items, totalCount) = await _repository.QueryAsync(query, ct);

		return new PagedResult<TaskItemDto>
		{
			Items = items.Select(e => ToDto(e, urgencyLevelName: e.UrgencyLevel?.Name)).ToList(),
			TotalCount = totalCount,
			Page = query.Page,
			PageSize = query.PageSize
		};
	}
	public async Task<TaskItemDto> UpdateAsync(Guid id, UpdateTaskItemDto dto, CancellationToken ct = default)
	{
		var entity = await _repository.GetByIdAsync(id, includeDeleted: false, ct)
			?? throw new TaskItemNotFoundException(id);

		if (!string.Equals(entity.Title, dto.Title, StringComparison.Ordinal)
			&& await _repository.ExistsWithActiveTitleAsync(dto.Title, excludingId: entity.Id, ct))
		{
			throw new TaskItemConflictException(
				"DUPLICATE_ACTIVE_TITLE",
				$"An active TaskItem with the title '{dto.Title}' already exists.");
		}

		if (!await _repository.UrgencyLevelExistsAsync(dto.UrgencyLevelId, ct))
		{
			throw new AppValidationException(nameof(dto.UrgencyLevelId), "Urgency level does not exist.");
		}

		// Full replace,-- but note Stage is NOT a field on
		// UpdateTaskItemDto at all. PUT is still a "normal update" under
		//  -- it can't move Stage backward either; Stage only ever
		// changes via ChangeStageAsync or ReopenAsync.
		entity.Title = dto.Title;
		entity.Notes = dto.Notes;
		entity.UrgencyLevelId = dto.UrgencyLevelId;
		entity.Deadline = dto.Deadline;
		entity.UpdatedAtUtc = DateTime.UtcNow;

		await _repository.SaveChangesAsync(ct);
		return ToDto(entity, urgencyLevelName: null);
	}

	public async Task<TaskItemDto> PatchAsync(Guid id, PatchTaskItemDto dto, CancellationToken ct = default)
	{
		var entity = await _repository.GetByIdAsync(id, includeDeleted: false, ct)
			?? throw new TaskItemNotFoundException(id);

		// -- only fields actually present in the JSON get touched.
		// Optional<T>.IsSet is what distinguishes "omitted" from "sent as null".
		if (dto.Title.IsSet)
		{
			var newTitle = dto.Title.Value ?? string.Empty;
			if (!string.Equals(entity.Title, newTitle, StringComparison.Ordinal)
				&& await _repository.ExistsWithActiveTitleAsync(newTitle, excludingId: entity.Id, ct))
			{
				throw new TaskItemConflictException(
					"DUPLICATE_ACTIVE_TITLE",
					$"An active TaskItem with the title '{newTitle}' already exists.");
			}
			entity.Title = newTitle;
		}

		if (dto.Notes.IsSet)
		{
			entity.Notes = dto.Notes.Value;
		}

		if (dto.UrgencyLevelId.IsSet)
		{
			if (!await _repository.UrgencyLevelExistsAsync(dto.UrgencyLevelId.Value, ct))
			{
				throw new AppValidationException(nameof(dto.UrgencyLevelId), "Urgency level does not exist.");
			}
			entity.UrgencyLevelId = dto.UrgencyLevelId.Value;
		}

		if (dto.Deadline.IsSet)
		{
			entity.Deadline = dto.Deadline.Value;
		}

		// Same as PUT: no Stage field exists on this DTO at all, so PATCH
		// physically cannot be used to change stage, forward or backward.
		entity.UpdatedAtUtc = DateTime.UtcNow;
		await _repository.SaveChangesAsync(ct);
		return ToDto(entity, urgencyLevelName: null);
	}
	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var entity = await _repository.GetByIdAsync(id, includeDeleted: false, ct)
			?? throw new TaskItemNotFoundException(id);

		// Rule 5.1: soft delete only. Support can restore within 90 days;
		// permanent removal is a separate, unbuilt purge job (see DECISIONS.md).
		entity.IsDeleted = true;
		entity.DeletedAtUtc = DateTime.UtcNow;
		entity.UpdatedAtUtc = DateTime.UtcNow;
		await _repository.SaveChangesAsync(ct);
	}

	public async Task<TaskItemDto> RestoreAsync(Guid id, CancellationToken ct = default)
	{
		var entity = await _repository.GetByIdAsync(id, includeDeleted: true, ct)
			?? throw new TaskItemNotFoundException(id);

		if (!entity.IsDeleted)
		{
			// Restoring something that isn't deleted isn't a meaningful
			// no-op like the idempotent stage-change case -- it's a caller
			// mistake worth surfacing, not silently swallowing.
			throw new TaskItemConflictException(
				"NOT_DELETED",
				$"TaskItem '{id}' is not deleted; nothing to restore.");
		}

		// Restoring re-activates the title -- uniqueness check
		// applies again here. Without this, restore could silently create
		// two active TaskItems with the same title if a new one was
		// created using this title while the original was deleted.
		if (await _repository.ExistsWithActiveTitleAsync(entity.Title, excludingId: entity.Id, ct))
		{
			throw new TaskItemConflictException(
				"DUPLICATE_ACTIVE_TITLE",
				$"Cannot restore: an active TaskItem with the title '{entity.Title}' already exists.");
		}

		entity.IsDeleted = false;
		entity.DeletedAtUtc = null;
		entity.UpdatedAtUtc = DateTime.UtcNow;
		await _repository.SaveChangesAsync(ct);

		return ToDto(entity, urgencyLevelName: null);
	}
	public async Task<TaskItemDto> ChangeStageAsync(Guid id, string targetStage, CancellationToken ct = default)
	{
		if (!Enum.TryParse<TaskStage>(targetStage, ignoreCase: true, out var target))
		{
			throw new AppValidationException(nameof(targetStage), $"'{targetStage}' is not a valid stage.");
		}

		var entity = await _repository.GetByIdAsync(id, includeDeleted: false, ct)
			?? throw new TaskItemNotFoundException(id);

		// Idempotency: same request arriving twice (mobile, bad connection)
		// is a no-op on the second call, not an error.
		if (entity.Stage == target)
		{
			return ToDto(entity, urgencyLevelName: null);
		}

		//forward-only under normal operation. Backward moves,
		// including from Finished, are rejected here -- Reopen() is the
		// only deliberate bypass, implemented separately.
		if (!entity.Stage.IsForwardMoveTo(target))
		{
			throw new TaskItemConflictException(
				"BACKWARD_STAGE_TRANSITION",
				$"Cannot move TaskItem from '{entity.Stage}' back to '{target}'. Use the reopen action if this is intentional.");
		}

		entity.Stage = target;
		entity.UpdatedAtUtc = DateTime.UtcNow;
		await _repository.SaveChangesAsync(ct);

		return ToDto(entity, urgencyLevelName: null);
	}
	public async Task<TaskItemDto> ReopenAsync(Guid id, CancellationToken ct = default)
	{
		var entity = await _repository.GetByIdAsync(id, includeDeleted: false, ct)
			?? throw new TaskItemNotFoundException(id);

		// "if backward is allowed at all, it must be a separate
		// deliberate action, not a side effect of a normal update." This IS
		// that action. Reopen only makes sense from Finished -- moving back
		// from InProgress to Started isn't what this rule is describing, and
		// ChangeStageAsync already blocks it the same way it blocks Finished.
		if (entity.Stage != TaskStage.Finished)
		{
			throw new TaskItemConflictException(
				"REOPEN_NOT_APPLICABLE",
				$"Reopen only applies to a Finished TaskItem; current stage is '{entity.Stage}'.");
		}

		entity.Stage = TaskStage.InProgress;
		entity.UpdatedAtUtc = DateTime.UtcNow;
		await _repository.SaveChangesAsync(ct);

		return ToDto(entity, urgencyLevelName: null);
	}
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