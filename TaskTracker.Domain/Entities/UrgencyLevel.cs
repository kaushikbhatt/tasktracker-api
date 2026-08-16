using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTracker.Domain.Entities;

/// <summary>
/// Urgency is modelled as reference data (a table),
/// </summary>
public class UrgencyLevel
{
    public int Id { get; set; }

    /// <summary>Human-readable name, e.g. "Low", "Medium", "High".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Determines default sort order (lower = less urgent). Also used
    /// for "sort by urgency" so ordering doesn't depend on Id insertion order.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
}