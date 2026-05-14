using FluentAssertions;
using Insequens.Application.Commands;

namespace Insequens.Application.Tests.Commands;

public class IOwnedTests
{
    [Fact]
    public void ImplementingType_ExposesOwnedResourceProperties()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        IOwned owned = new TestOwnedRequest(userId, itemId);

        owned.UserId.Should().Be(userId);
        owned.ItemId.Should().Be(itemId);
    }

    private sealed record TestOwnedRequest(Guid UserId, Guid ItemId) : IOwned;
}
