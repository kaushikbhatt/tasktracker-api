using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence;

public class TaskTrackerDbContext : DbContext
{
	public TaskTrackerDbContext(DbContextOptions<TaskTrackerDbContext> options) : base(options) { }

	public DbSet<TaskItem> TaskItems => Set<TaskItem>();
	public DbSet<UrgencyLevel> UrgencyLevels => Set<UrgencyLevel>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskTrackerDbContext).Assembly);
	}
}