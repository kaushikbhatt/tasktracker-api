using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTracker.Application.DTOs;

/// <summary>POST /task-items body. Every field the client is allowed to set on create.</summary>
public sealed class CreateTaskItemDto
{
	public string Title { get; set; } = string.Empty;
	public string? Notes { get; set; }
	public int UrgencyLevelId { get; set; }
	public DateTime? Deadline { get; set; }
}

/// <summary>
/// PUT /task-items/{id} body -- full replace.	
/// </summary>
public sealed class UpdateTaskItemDto
{
	public string Title { get; set; } = string.Empty;
	public string? Notes { get; set; }
	public int UrgencyLevelId { get; set; }
	public DateTime? Deadline { get; set; }
}

/// <summary>
/// PATCH /task-items/{id} body. Every field is Optional -- only fields
/// </summary>
public sealed class PatchTaskItemDto
{
	public Optional<string> Title { get; set; }
	public Optional<string?> Notes { get; set; }
	public Optional<int> UrgencyLevelId { get; set; }
	public Optional<DateTime?> Deadline { get; set; }
}

/// <summary>POST /task-items/{id}/stage body -- mobile's single-field, idempotent action.</summary>
public sealed class ChangeStageDto
{
	public string TargetStage { get; set; } = string.Empty;
}

/// <summary>
/// GET /task-items query parameters. Supports filtering by stage, urgency,
/// and deadline range in any combination plus sort and paging.
/// </summary>
public sealed class TaskItemQuery
{
	public string? Stage { get; set; }
	public int? UrgencyLevelId { get; set; }
	public DateTime? DeadlineFrom { get; set; }
	public DateTime? DeadlineTo { get; set; }
	public bool IncludeDeleted { get; set; }

	/// <summary>"urgency" or "deadline".</summary>
	public string? SortBy { get; set; }
	public bool SortDescending { get; set; }

	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 50;
}

/// <summary>
/// Wraps a page of results with the total match count -- what the filter
/// </summary>
public sealed class PagedResult<T>
{
	public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
	public int TotalCount { get; set; }
	public int Page { get; set; }
	public int PageSize { get; set; }
}