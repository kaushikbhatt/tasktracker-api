using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Exceptions;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Infrastructure.Persistence.Repositories;

public sealed class TaskItemRepository : ITaskItemRepository
{
    private readonly TaskTrackerDbContext _db;

    public TaskItemRepository(TaskTrackerDbContext db)
    {
        _db = db;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var query = _db.TaskItems
            .Include(t => t.UrgencyLevel)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(t => !t.IsDeleted);
        }

        return await query.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<bool> ExistsWithActiveTitleAsync(string title, Guid? excludingId = null, CancellationToken ct = default)
    {
        return await _db.TaskItems
            .AsNoTracking()
            .Where(t => !t.IsDeleted && EF.Functions.Collate(t.Title, "NOCASE") == title)
            .Where(t => !excludingId.HasValue || t.Id != excludingId.Value)
            .AnyAsync(ct);
    }

    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> QueryAsync(TaskItemQuery query, CancellationToken ct = default)
    {
        var q = _db.TaskItems
            .AsNoTracking()
            .AsQueryable()
            .TagWith("TaskItemRepository.QueryAsync base");

        // Validate deadline range early to fail fast on bad input
        if (query.DeadlineFrom.HasValue && query.DeadlineTo.HasValue && query.DeadlineFrom > query.DeadlineTo)
        {
            throw new AppValidationException(nameof(query.DeadlineFrom), "DeadlineFrom must be less than or equal to DeadlineTo.");
        }

        if (!query.IncludeDeleted)
        {
            q = q.Where(t => !t.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(query.Stage))
        {
            if (!Enum.TryParse<TaskStage>(query.Stage, true, out var stage))
            {
                throw new AppValidationException(nameof(query.Stage), $"'{query.Stage}' is not a valid stage.");
            }
            q = q.Where(t => t.Stage == stage);
        }

        if (query.UrgencyLevelId.HasValue)
        {
            q = q.Where(t => t.UrgencyLevelId == query.UrgencyLevelId.Value);
        }

        if (query.DeadlineFrom.HasValue)
        {
            var from = query.DeadlineFrom.Value;
            q = q.Where(t => t.Deadline != null && t.Deadline >= from);
        }

        if (query.DeadlineTo.HasValue)
        {
            var to = query.DeadlineTo.Value;
            q = q.Where(t => t.Deadline != null && t.Deadline <= to);
        }

        var sortBy = query.SortBy?.Trim().ToLowerInvariant();
        bool desc = query.SortDescending;
        q = sortBy switch
        {
            "urgency" => desc
                ? q.OrderByDescending(t => t.UrgencyLevel!.SortOrder).ThenByDescending(t => t.UpdatedAtUtc).ThenBy(t => t.Id)
                : q.OrderBy(t => t.UrgencyLevel!.SortOrder).ThenByDescending(t => t.UpdatedAtUtc).ThenBy(t => t.Id),
            "deadline" => desc
                ? q.OrderByDescending(t => t.Deadline.HasValue).ThenByDescending(t => t.Deadline).ThenBy(t => t.Id)
                : q.OrderBy(t => t.Deadline == null).ThenBy(t => t.Deadline).ThenBy(t => t.Id),
            null or "" => q.OrderByDescending(t => t.CreatedAtUtc).ThenBy(t => t.Id),
            _ => throw new AppValidationException(nameof(query.SortBy), "SortBy must be 'urgency' or 'deadline'.")
        };

        var total = await q.CountAsync(ct);
        var skip = (query.Page - 1) * query.PageSize;
        var items = await q.Skip(skip).Take(query.PageSize)
            .Include(t => t.UrgencyLevel)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(TaskItem taskItem, CancellationToken ct = default)
    {
        await _db.TaskItems.AddAsync(taskItem, ct);
    }

    public async Task<IReadOnlyList<UrgencyLevel>> GetUrgencyLevelsAsync(CancellationToken ct = default)
    {
        return await _db.UrgencyLevels
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<bool> UrgencyLevelExistsAsync(int urgencyLevelId, CancellationToken ct = default)
    {
        return await _db.UrgencyLevels.AsNoTracking().AnyAsync(u => u.Id == urgencyLevelId && u.IsActive, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}