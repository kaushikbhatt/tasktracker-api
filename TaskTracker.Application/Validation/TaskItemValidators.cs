using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Validation;

/// <summary>
/// Field-shape validation only -- is the title too long, is it missing.
/// Business rules (is this title already taken, is this stage move allowed)
/// live in TaskItemService, not here. This is rule 5.5's first line of
/// defence: FluentValidation runs before the service is ever called.
/// </summary>
public sealed class CreateTaskItemDtoValidator : AbstractValidator<CreateTaskItemDto>
{
	public CreateTaskItemDtoValidator()
	{
		RuleFor(x => x.Title)
			.NotEmpty().WithMessage("Title is required.")
			.MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");

		RuleFor(x => x.Notes)
			.MaximumLength(500).WithMessage("Notes must be 500 characters or fewer.");

		RuleFor(x => x.UrgencyLevelId)
			.GreaterThan(0).WithMessage("UrgencyLevelId is required.");
	}
}

public sealed class UpdateTaskItemDtoValidator : AbstractValidator<UpdateTaskItemDto>
{
	public UpdateTaskItemDtoValidator()
	{
		RuleFor(x => x.Title)
			.NotEmpty().WithMessage("Title is required.")
			.MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");

		RuleFor(x => x.Notes)
			.MaximumLength(500).WithMessage("Notes must be 500 characters or fewer.");

		RuleFor(x => x.UrgencyLevelId)
			.GreaterThan(0).WithMessage("UrgencyLevelId is required.");
	}
}

public sealed class PatchTaskItemDtoValidator : AbstractValidator<PatchTaskItemDto>
{
	public PatchTaskItemDtoValidator()
	{
		// Only validate fields the caller actually set -- Optional<T>.IsSet
		// is what makes this "partial" validation, matching rule 5.4.
		When(x => x.Title.IsSet, () =>
		{
			RuleFor(x => x.Title.Value)
				.NotEmpty().WithMessage("Title cannot be set to empty.")
				.MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");
		});

		When(x => x.Notes.IsSet, () =>
		{
			RuleFor(x => x.Notes.Value)
				.MaximumLength(500).WithMessage("Notes must be 500 characters or fewer.");
		});

		When(x => x.UrgencyLevelId.IsSet, () =>
		{
			RuleFor(x => x.UrgencyLevelId.Value)
				.GreaterThan(0).WithMessage("UrgencyLevelId must be a positive number.");
		});
	}
}