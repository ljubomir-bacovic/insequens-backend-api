using Insequens.Domain.Entities;
using Insequens.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Insequens.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyKeys");
        builder.HasKey(record => record.Id);
        builder.Ignore(record => record.IsCompleted);

        builder.Property(record => record.Key).HasMaxLength(IdempotencyRecord.KeyMaxLength).IsRequired();
        builder.Property(record => record.RequestHash)
            .HasMaxLength(IdempotencyRecord.RequestHashLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();

        // Two concurrent requests with the same key: the second INSERT fails, so the request runs once. Two concurrent
        // restarts of an expired key: every change moves ExpiresAt, so the second UPDATE matches no row and fails.
        builder.HasIndex(record => new { record.UserId, record.Key }).IsUnique();
        builder.Property(record => record.ExpiresAt).IsConcurrencyToken();
        builder.HasIndex(record => record.ExpiresAt);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
