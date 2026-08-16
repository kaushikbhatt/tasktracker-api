using System;
using System.Collections.Generic;
using System.Text;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Interfaces;

/// <summary>
/// Persistence contract for TaskItem. Deliberately NOT a generic
/// </summary>
public interface ITaskItemRepository
{
	Task<TaskItem?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);

	Task<bool> ExistsWithActiveTitleAsync(string title, Guid? excludingId = null, CancellationToken ct = default);

	Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> QueryAsync(TaskItemQuery query, CancellationToken ct = default);

	Task AddAsync(TaskItem taskItem, CancellationToken ct = default);

	Task<IReadOnlyList<UrgencyLevel>> GetUrgencyLevelsAsync(CancellationToken ct = default);

	Task<bool> UrgencyLevelExistsAsync(int urgencyLevelId, CancellationToken ct = default);

	/// <summary>Commits pending changes. This IS the unit of work -- see
	/// DECISIONS.md on why no separate IUnitOfWork wrapper was added.</summary>
	Task SaveChangesAsync(CancellationToken ct = default);
}