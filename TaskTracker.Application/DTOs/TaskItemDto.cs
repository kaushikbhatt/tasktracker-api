using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTracker.Application.DTOs;

public sealed class TaskItemDto
{
	public Guid Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Notes { get; set; }
	public string Stage { get; set; } = string.Empty;
	public int UrgencyLevelId { get; set; }
	public string UrgencyLevelName { get; set; } = string.Empty;
	public DateTime? Deadline { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public DateTime UpdatedAtUtc { get; set; }
	public bool IsDeleted { get; set; }
}

public sealed class UrgencyLevelDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public int SortOrder { get; set; }
}