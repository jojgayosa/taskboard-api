using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Persistence.Configurations
{
    public class TaskColumnConfiguration : IEntityTypeConfiguration<TaskColumn>
    {
        public void Configure(EntityTypeBuilder<TaskColumn> builder)
        {
            builder.ToTable("TaskColumns");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasOne(t => t.Project)
                .WithMany(t => t.Columns)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            
        }
    }
}
