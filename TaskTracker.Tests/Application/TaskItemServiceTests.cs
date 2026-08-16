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
}
