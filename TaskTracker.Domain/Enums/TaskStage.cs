using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTracker.Domain.Enums
{
	/// <summary>
	/// The lifecycle of a TaskItem Started -> InProgress -> Finished. 
	/// </summary>
	public enum TaskStage
	{
		Started = 0,
		InProgress = 1,
		Finished = 2
	}
}
