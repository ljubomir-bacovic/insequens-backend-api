using Insequens.Domain.Entities;
using Insequens.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Insequens.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(nameof(RefreshToken));
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash).HasMaxLength(RefreshToken.TokenHashLength).IsUnicode(false);
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(RefreshToken.TokenHashLength).IsUnicode(false);
        builder.Property(token => token.DeviceName).HasMaxLength(RefreshToken.DeviceNameMaxLength);
        builder.Property(token => token.CreatedByIp).HasMaxLength(RefreshToken.IpAddressMaxLength).IsUnicode(false);

        // Two concurrent refreshes with the same token: the second UPDATE matches no row and fails, so a token
        // is never rotated twice.
        builder.Property(token => token.RevokedAt).IsConcurrencyToken();

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.FamilyId });
        builder.HasIndex(token => token.ExpiresAt);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
