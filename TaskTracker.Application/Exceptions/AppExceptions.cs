using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTracker.Application.Exceptions;

/// <summary>
/// Base type for exceptions the middleware
/// ErrorCode is the stable identifier other systems key off of; Message is
/// the human-readable, field-specific text.
/// </summary>
public abstract class AppException : Exception
{
	public abstract string ErrorCode { get; }
	protected AppException(string message) : base(message) { }
}

/// <summary>404 - the requested TaskItem does not exist (or is soft-deleted
/// and the caller didn't ask to include deleted items).</summary>
public sealed class TaskItemNotFoundException : AppException
{
	public override string ErrorCode => "TASK_ITEM_NOT_FOUND";
	public TaskItemNotFoundException(Guid id) : base($"TaskItem '{id}' was not found.") { }
}

/// <summary>409 - the requested change conflicts with current state, e.g. a
/// duplicate active title, or a forward-only stage rule violation.</summary>
public sealed class TaskItemConflictException : AppException
{
	public override string ErrorCode { get; }
	public TaskItemConflictException(string errorCode, string message) : base(message)
	{
		ErrorCode = errorCode;
	}
}

/// <summary>
/// 400 - field-level validation failure. Carries a dictionary of
/// field -> messages so the response can say exactly which field was wrong
/// </summary>
public sealed class AppValidationException : AppException
{
	public override string ErrorCode => "VALIDATION_FAILED";
	public IReadOnlyDictionary<string, string[]> Errors { get; }

	public AppValidationException(IReadOnlyDictionary<string, string[]> errors)
		: base("One or more fields are invalid.")
	{
		Errors = errors;
	}

	public AppValidationException(string field, string message)
		: this(new Dictionary<string, string[]> { [field] = new[] { message } }) { }
}
