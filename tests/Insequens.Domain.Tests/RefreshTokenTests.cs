using FluentAssertions;
using Insequens.Domain.Entities;
using Insequens.Domain.Exceptions;

namespace Insequens.Domain.Tests;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Issue_StartsANewActiveSession()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now.AddDays(7), "Phone", "203.0.113.7");

        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(UserId);
        token.TokenHash.Should().Be("hash");
        token.FamilyId.Should().NotBeEmpty();
        token.ExpiresAt.Should().Be(Now.AddDays(7));
        token.DeviceName.Should().Be("Phone");
        token.CreatedByIp.Should().Be("203.0.113.7");
        token.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void Issue_Twice_StartsTwoSessions()
    {
        var first = RefreshToken.Issue(UserId, "hash-1", Now.AddDays(7), null, null);
        var second = RefreshToken.Issue(UserId, "hash-2", Now.AddDays(7), null, null);

        first.FamilyId.Should().NotBe(second.FamilyId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Issue_WithoutHash_Throws(string hash)
    {
        var action = () => RefreshToken.Issue(UserId, hash, Now.AddDays(7), null, null);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_WithBlankDeviceNameAndOverlongAddress_StoresNoNameAndTruncatesTheAddress()
    {
        var address = "fe80:0000:0000:0000:0000:0000:0000:0001%interface-with-a-long-name";

        var token = RefreshToken.Issue(UserId, "hash", Now.AddDays(7), "   ", address);

        token.DeviceName.Should().BeNull();
        token.CreatedByIp.Should().Be(address[..RefreshToken.IpAddressMaxLength]);
    }

    [Fact]
    public void IsActive_AtExpiry_IsFalse()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now.AddDays(7), null, null);

        token.IsActive(Now.AddDays(7)).Should().BeFalse();
    }

    [Fact]
    public void Rotate_RetiresTheTokenAndReturnsItsSuccessorInTheSameSession()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now.AddDays(7), "Phone", "203.0.113.7");

        var successor = token.Rotate("next-hash", Now.AddDays(8), "198.51.100.4", Now.AddDays(1));

        token.RevokedAt.Should().Be(Now.AddDays(1));
        token.ReplacedByTokenHash.Should().Be("next-hash");
        token.IsActive(Now.AddDays(1)).Should().BeFalse();
        successor.Id.Should().NotBe(token.Id);
        successor.UserId.Should().Be(UserId);
        successor.FamilyId.Should().Be(token.FamilyId);
        successor.TokenHash.Should().Be("next-hash");
        successor.ExpiresAt.Should().Be(Now.AddDays(8));
        successor.DeviceName.Should().Be("Phone");
        successor.CreatedByIp.Should().Be("198.51.100.4");
        successor.IsActive(Now.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void Rotate_WhenAlreadyRotated_Throws()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now.AddDays(7), null, null);
        token.Rotate("next-hash", Now.AddDays(8), null, Now);

        var action = () => token.Rotate("another-hash", Now.AddDays(8), null, Now);

        action.Should().Throw<RefreshTokenNotActiveException>();
    }

    [Fact]
    public void Rotate_WhenExpired_Throws()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now.AddDays(7), null, null);

        var action = () => token.Rotate("next-hash", Now.AddDays(14), null, Now.AddDays(7));

        action.Should().Throw<RefreshTokenNotActiveException>();
        token.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Revoke_Twice_KeepsTheFirstRevocationTime()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now.AddDays(7), null, null);

        token.Revoke(Now);
        token.Revoke(Now.AddHours(1));

        token.RevokedAt.Should().Be(Now);
        token.IsActive(Now).Should().BeFalse();
    }
}
