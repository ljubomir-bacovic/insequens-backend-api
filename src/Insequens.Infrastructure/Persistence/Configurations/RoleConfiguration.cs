using Insequens.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Insequens.Infrastructure.Persistence.Configurations;

/// <summary>Seeds the fixed roles. IDs and concurrency stamps are constants so the model does not change between builds.</summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public static readonly Guid AdminRoleId = Guid.Parse("8d0c6c39-2f4e-4c1a-9a57-3b8f1e2d4a01");
    public static readonly Guid SupportRoleId = Guid.Parse("8d0c6c39-2f4e-4c1a-9a57-3b8f1e2d4a02");

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.HasData(
            Role(AdminRoleId, Roles.Admin),
            Role(SupportRoleId, Roles.Support));
    }

    private static IdentityRole<Guid> Role(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id.ToString(),
    };
}
