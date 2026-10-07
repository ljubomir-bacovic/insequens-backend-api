using Insequens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Insequens.Infrastructure.Persistence.Configurations;

public sealed class ToDoItemConfiguration : IEntityTypeConfiguration<ToDoItem>
{
    public void Configure(EntityTypeBuilder<ToDoItem> builder)
    {
        builder.ToTable(nameof(ToDoItem));
        builder.HasKey(item => item.Id);
    }
}
