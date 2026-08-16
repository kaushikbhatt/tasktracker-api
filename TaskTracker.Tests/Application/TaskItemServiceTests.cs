using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Moq;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Exceptions;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Services;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using Xunit;

namespace TaskTracker.Tests.Application;

public class TaskItemServiceTests
{
	private readonly Mock<ITaskItemRepository> _repo = new();
	private readonly TaskItemService _sut;

	public TaskItemServiceTests()
	{
		_sut = new TaskItemService(_repo.Object);
	}

	// ---------- Create: duplicate title  ----------

	[Fact]
	public async Task Create_WithDuplicateActiveTitle_ThrowsConflict()
	{
		_repo.Setup(r => r.ExistsWithActiveTitleAsync("Inspect Line 12", null, default))
			.ReturnsAsync(true);

		var dto = new CreateTaskItemDto { Title = "Inspect Line 12", UrgencyLevelId = 1 };

		var act = async () => await _sut.CreateAsync(dto);

		(await act.Should().ThrowAsync<TaskItemConflictException>())
			.Which.ErrorCode.Should().Be("DUPLICATE_ACTIVE_TITLE");
	}

	[Fact]
	public async Task Create_WithUniqueTitle_Succeeds()
	{
		_repo.Setup(r => r.ExistsWithActiveTitleAsync(It.IsAny<string>(), null, default))
			.ReturnsAsync(false);
		_repo.Setup(r => r.UrgencyLevelExistsAsync(1, default)).ReturnsAsync(true);

		var dto = new CreateTaskItemDto { Title = "New Task", UrgencyLevelId = 1 };
		var result = await _sut.CreateAsync(dto);

		result.Title.Should().Be("New Task");
		result.Stage.Should().Be(nameof(TaskStage.Started));
		_repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), default), Times.Once);
	}

	// ---------- Forward-only stage transition ----------

	[Fact]
	public async Task ChangeStage_Backward_ThrowsConflict()
	{
		var entity = new TaskItem { Id = Guid.NewGuid(), Stage = TaskStage.Finished, Title = "X" };
		_repo.Setup(r => r.GetByIdAsync(entity.Id, false, default)).ReturnsAsync(entity);

		var act = async () => await _sut.ChangeStageAsync(entity.Id, nameof(TaskStage.Started));

		(await act.Should().ThrowAsync<TaskItemConflictException>())
			.Which.ErrorCode.Should().Be("BACKWARD_STAGE_TRANSITION");
	}

	[Fact]
	public async Task ChangeStage_SameStageTwice_IsIdempotentNoOp()
	{
		var entity = new TaskItem { Id = Guid.NewGuid(), Stage = TaskStage.InProgress, Title = "X" };
		_repo.Setup(r => r.GetByIdAsync(entity.Id, false, default)).ReturnsAsync(entity);

		var result = await _sut.ChangeStageAsync(entity.Id, nameof(TaskStage.InProgress));

		result.Stage.Should().Be(nameof(TaskStage.InProgress));
		_repo.Verify(r => r.SaveChangesAsync(default), Times.Never); // no write happened
	}

	[Fact]
	public async Task Reopen_FromFinished_MovesToInProgress()
	{
		var entity = new TaskItem { Id = Guid.NewGuid(), Stage = TaskStage.Finished, Title = "X" };
		_repo.Setup(r => r.GetByIdAsync(entity.Id, false, default)).ReturnsAsync(entity);

		var result = await _sut.ReopenAsync(entity.Id);

		result.Stage.Should().Be(nameof(TaskStage.InProgress));
	}

	[Fact]
	public async Task Reopen_WhenNotFinished_ThrowsConflict()
	{
		var entity = new TaskItem { Id = Guid.NewGuid(), Stage = TaskStage.Started, Title = "X" };
		_repo.Setup(r => r.GetByIdAsync(entity.Id, false, default)).ReturnsAsync(entity);

		var act = async () => await _sut.ReopenAsync(entity.Id);

		(await act.Should().ThrowAsync<TaskItemConflictException>())
			.Which.ErrorCode.Should().Be("REOPEN_NOT_APPLICABLE");
	}

	// ---------- Soft delete / restore (restore-uniqueness edge case) ----------

	[Fact]
	public async Task Delete_SetsIsDeletedAndTimestamp()
	{
		var entity = new TaskItem { Id = Guid.NewGuid(), Title = "X" };
		_repo.Setup(r => r.GetByIdAsync(entity.Id, false, default)).ReturnsAsync(entity);

		await _sut.DeleteAsync(entity.Id);

		entity.IsDeleted.Should().BeTrue();
		entity.DeletedAtUtc.Should().NotBeNull();
	}

	[Fact]
	public async Task Restore_WhenDuplicateActiveTitleExists_ThrowsConflict()
	{
		var entity = new TaskItem { Id = Guid.NewGuid(), Title = "Inspect Line 12", IsDeleted = true };
		_repo.Setup(r => r.GetByIdAsync(entity.Id, true, default)).ReturnsAsync(entity);
		_repo.Setup(r => r.ExistsWithActiveTitleAsync("Inspect Line 12", entity.Id, default))
			.ReturnsAsync(true);

		var act = async () => await _sut.RestoreAsync(entity.Id);

		(await act.Should().ThrowAsync<TaskItemConflictException>())
			.Which.ErrorCode.Should().Be("DUPLICATE_ACTIVE_TITLE");
	}

		[Fact]
	public async Task Restore_WhenTitleFree_Succeeds()
	{
		var entity = new TaskItem { Id = Guid.NewGuid(), Title = "Inspect Line 12", IsDeleted = true, DeletedAtUtc = DateTime.UtcNow };
		_repo.Setup(r => r.GetByIdAsync(entity.Id, true, default)).ReturnsAsync(entity);
		_repo.Setup(r => r.ExistsWithActiveTitleAsync("Inspect Line 12", entity.Id, default))
			.ReturnsAsync(false);

		await _sut.RestoreAsync(entity.Id);

		entity.IsDeleted.Should().BeFalse();
		entity.DeletedAtUtc.Should().BeNull();
	}

	// ---------- Update: title uniqueness and urgency validation ----------

	[Fact]
	public async Task Update_WhenChangingTitleToDuplicate_ThrowsConflict()
	{
		var id = Guid.NewGuid();
		var entity = new TaskItem { Id = id, Title = "Old", UrgencyLevelId = 1 };
		_repo.Setup(r => r.GetByIdAsync(id, false, default)).ReturnsAsync(entity);
		_repo.Setup(r => r.ExistsWithActiveTitleAsync("New", id, default)).ReturnsAsync(true);

		var dto = new UpdateTaskItemDto { Title = "New", UrgencyLevelId = 1 };
		var act = async () => await _sut.UpdateAsync(id, dto);

		(await act.Should().ThrowAsync<TaskItemConflictException>())
			.Which.ErrorCode.Should().Be("DUPLICATE_ACTIVE_TITLE");
	}

	[Fact]
	public async Task Update_WithValidChanges_Succeeds()
	{
		var id = Guid.NewGuid();
		var entity = new TaskItem { Id = id, Title = "Old", UrgencyLevelId = 1 };
		_repo.Setup(r => r.GetByIdAsync(id, false, default)).ReturnsAsync(entity);
		_repo.Setup(r => r.ExistsWithActiveTitleAsync(It.IsAny<string>(), id, default)).ReturnsAsync(false);
		_repo.Setup(r => r.UrgencyLevelExistsAsync(2, default)).ReturnsAsync(true);

		var dto = new UpdateTaskItemDto { Title = "New Title", Notes = "N", UrgencyLevelId = 2 };
		var result = await _sut.UpdateAsync(id, dto);

		result.Title.Should().Be("New Title");
		entity.UrgencyLevelId.Should().Be(2);
		_repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
	}

	// ---------- Patch: title uniqueness and urgency validation ----------

	[Fact]
	public async Task Patch_WhenChangingTitleToDuplicate_ThrowsConflict()
	{
		var id = Guid.NewGuid();
		var entity = new TaskItem { Id = id, Title = "Old", UrgencyLevelId = 1 };
		_repo.Setup(r => r.GetByIdAsync(id, false, default)).ReturnsAsync(entity);
		_repo.Setup(r => r.ExistsWithActiveTitleAsync("Clash", id, default)).ReturnsAsync(true);

		var dto = new PatchTaskItemDto { Title = Optional<string>.Of("Clash") };
		var act = async () => await _sut.PatchAsync(id, dto);

		(await act.Should().ThrowAsync<TaskItemConflictException>())
			.Which.ErrorCode.Should().Be("DUPLICATE_ACTIVE_TITLE");
	}

	[Fact]
	public async Task Patch_WhenUrgencyInvalid_ThrowsValidation()
	{
		var id = Guid.NewGuid();
		var entity = new TaskItem { Id = id, Title = "X", UrgencyLevelId = 1 };
		_repo.Setup(r => r.GetByIdAsync(id, false, default)).ReturnsAsync(entity);
		_repo.Setup(r => r.UrgencyLevelExistsAsync(999, default)).ReturnsAsync(false);

		var dto = new PatchTaskItemDto { UrgencyLevelId = Optional<int>.Of(999) };
		var act = async () => await _sut.PatchAsync(id, dto);

		(await act.Should().ThrowAsync<AppValidationException>())
			.Which.ErrorCode.Should().Be("VALIDATION_FAILED");
	}

	// ---------- Query: paging validation ----------

	[Fact]
	public async Task Query_WhenPageLessThanOne_ThrowsValidation()
	{
		var q = new TaskItemQuery { Page = 0, PageSize = 10 };
		var act = async () => await _sut.QueryAsync(q);
		(await act.Should().ThrowAsync<AppValidationException>())
			.Which.ErrorCode.Should().Be("VALIDATION_FAILED");
	}

	[Fact]
	public async Task Query_WhenPageSizeOutOfRange_ThrowsValidation()
	{
		var q1 = new TaskItemQuery { Page = 1, PageSize = 0 };
		var q2 = new TaskItemQuery { Page = 1, PageSize = 201 };

		var act1 = async () => await _sut.QueryAsync(q1);
		var act2 = async () => await _sut.QueryAsync(q2);

		(await act1.Should().ThrowAsync<AppValidationException>()).Which.ErrorCode.Should().Be("VALIDATION_FAILED");
		(await act2.Should().ThrowAsync<AppValidationException>()).Which.ErrorCode.Should().Be("VALIDATION_FAILED");
	}

	// ---------- ChangeStage: invalid target ----------

	[Fact]
	public async Task ChangeStage_WithInvalidTarget_ThrowsValidation()
	{
		var id = Guid.NewGuid();
		var entity = new TaskItem { Id = id, Title = "X", Stage = TaskStage.Started };
		_repo.Setup(r => r.GetByIdAsync(id, false, default)).ReturnsAsync(entity);

		var act = async () => await _sut.ChangeStageAsync(id, "NotAStage");
		(await act.Should().ThrowAsync<AppValidationException>())
			.Which.ErrorCode.Should().Be("VALIDATION_FAILED");
	}

	// ---------- Restore: not-deleted case ----------

	[Fact]
	public async Task Restore_WhenNotDeleted_ThrowsConflict()
	{
		var id = Guid.NewGuid();
		var entity = new TaskItem { Id = id, Title = "X", IsDeleted = false };
		_repo.Setup(r => r.GetByIdAsync(id, true, default)).ReturnsAsync(entity);

		var act = async () => await _sut.RestoreAsync(id);
		(await act.Should().ThrowAsync<TaskItemConflictException>())
			.Which.ErrorCode.Should().Be("NOT_DELETED");
	}

	// ---------- GetById: not found ----------

	[Fact]
	public async Task GetById_WhenMissing_ThrowsNotFound()
	{
		var id = Guid.NewGuid();
		_repo.Setup(r => r.GetByIdAsync(id, false, default)).ReturnsAsync((TaskItem?)null);

		var act = async () => await _sut.GetByIdAsync(id);
		await act.Should().ThrowAsync<TaskItemNotFoundException>();
	}
}

