using Insequens.Domain.Entities;
using Insequens.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Insequens.Infrastructure.Persistence.Configurations;

public sealed class ToDoItemConfiguration : IEntityTypeConfiguration<ToDoItem>
{
    public const string TableName = "Tasks";
    public const string PriorityCheckConstraint = "CK_Tasks_Priority";

    public void Configure(EntityTypeBuilder<ToDoItem> builder)
    {
        builder.ToTable(TableName, table =>
            // None (0, from INS-030) and the three levels; NULL passes a check constraint.
            table.HasCheckConstraint(PriorityCheckConstraint, "[Priority] IN (0, 1, 2, 3)"));
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name).HasMaxLength(ToDoItem.NameMaxLength).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(ToDoItem.DescriptionMaxLength);

        // SQL Server rowversion: a concurrency token the database changes on every update.
        builder.Property(item => item.RowVersion).IsRowVersion();

        // The task list filters by owner and completion and sorts by due date; the export and future
        // "recently created" views read by owner and creation time.
        builder.HasIndex(item => new { item.UserId, item.IsCompleted, item.DueDate });
        builder.HasIndex(item => new { item.UserId, item.CreatedOn });

        // A task cannot outlive its owner: deleting an account (INS-045) deletes its tasks.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
