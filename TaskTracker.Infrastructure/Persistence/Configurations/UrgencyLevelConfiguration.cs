using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class UrgencyLevelConfiguration : IEntityTypeConfiguration<UrgencyLevel>
{
	public void Configure(EntityTypeBuilder<UrgencyLevel> builder)
	{
		builder.ToTable("UrgencyLevels");
		builder.HasKey(u => u.Id);

		builder.Property(u => u.Name)
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(u => u.IsActive).HasDefaultValue(true);

		// Seeded rows -- adding a 4th level next quarter is an INSERT here,
		// not a code change. See DECISIONS.md.
		builder.HasData(
			new UrgencyLevel { Id = 1, Name = "Low", SortOrder = 1, IsActive = true },
			new UrgencyLevel { Id = 2, Name = "Medium", SortOrder = 2, IsActive = true },
			new UrgencyLevel { Id = 3, Name = "High", SortOrder = 3, IsActive = true }
		);
	}
}