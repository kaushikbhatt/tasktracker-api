using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTracker.Domain.Enums;

/// <summary>
/// Encodes "forward" for TaskStage. Kept next to the enum in Domain (not in
/// Application) because "what counts as forward" is a fact about the
/// lifecycle itself, not a business policy about who's allowed to change it.
/// </summary>
public static class TaskStageExtensions
{
	public static bool IsForwardMoveTo(this TaskStage current, TaskStage target) =>
		target > current;
}