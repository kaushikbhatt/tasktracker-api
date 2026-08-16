using System;
using System.Collections.Generic;
using System.Text;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

/// <summary>
/// A unit of work assigned to operations staff.
/// </summary>
public class TaskItem
{
	public Guid Id { get; set; }

	/// <summary>Short human-readable title. Required. Unique among active
	/// (non-deleted) TaskItems only - rule 5.2.</summary>
	public string Title { get; set; } = string.Empty;

	/// <summary>Optional free-form notes. Most TaskItems will not have any.</summary>
	public string? Notes { get; set; }

	public TaskStage Stage { get; set; } = TaskStage.Started;

	public int UrgencyLevelId { get; set; }
	public UrgencyLevel? UrgencyLevel { get; set; }

	/// <summary>Optional deadline, stored in UTC.</summary>
	public DateTime? Deadline { get; set; }

	/// <summary>Server-assigned, never trusted from the client - rule 5 (final bullet).</summary>
	public DateTime CreatedAtUtc { get; set; }

	/// <summary>Server-assigned on every write, never trusted from the client.</summary>
	public DateTime UpdatedAtUtc { get; set; }

	/// <summary>Soft-delete flag. Deleted items are recoverable for 90 days - rule 5.1.</summary>
	public bool IsDeleted { get; set; }

	/// <summary>Set at the moment of soft delete; drives the 90-day purge window.</summary>
	public DateTime? DeletedAtUtc { get; set; }
}
