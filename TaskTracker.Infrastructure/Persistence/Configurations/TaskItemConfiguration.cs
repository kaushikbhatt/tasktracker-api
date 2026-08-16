using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
	public void Configure(EntityTypeBuilder<TaskItem> builder)
	{
		builder.ToTable("TaskItems");

		builder.HasKey(t => t.Id);

		builder.Property(t => t.Title)
			.HasMaxLength(200)
			.IsRequired()
			.UseCollation("NOCASE"); // Case-insensitive comparisons in SQLite
		;

		builder.Property(t => t.Notes)
			.HasMaxLength(500);

		builder.Property(t => t.Stage)
			.IsRequired();

		builder.Property(t => t.CreatedAtUtc).IsRequired();
		builder.Property(t => t.UpdatedAtUtc).IsRequired();
		builder.Property(t => t.IsDeleted).HasDefaultValue(false);

		builder.HasOne(t => t.UrgencyLevel)
			.WithMany(u => u.TaskItems)
			.HasForeignKey(t => t.UrgencyLevelId)
			.OnDelete(DeleteBehavior.Restrict); // never cascade-delete task history

		// unique among ACTIVE items only. Deleted rows are
		// invisible to this index, so a reused title never conflicts with
		// its own deleted predecessor. SQLite filter syntax confirmed in
		// the next commit (fix: correct SQLite filter syntax).
		builder.HasIndex(t => t.Title)
		   .IsUnique()
		   .HasFilter("\"IsDeleted\" = 0")
		   .HasDatabaseName("UX_TaskItems_Title_Active");

		builder.HasIndex(t => t.Stage).HasDatabaseName("IX_TaskItems_Stage");
		builder.HasIndex(t => t.UrgencyLevelId).HasDatabaseName("IX_TaskItems_UrgencyLevelId");
		builder.HasIndex(t => t.Deadline).HasDatabaseName("IX_TaskItems_Deadline");
		builder.HasIndex(t => new { t.IsDeleted, t.DeletedAtUtc }).HasDatabaseName("IX_TaskItems_IsDeleted_DeletedAtUtc");
	}
}