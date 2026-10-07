using Insequens.Application.Abstractions;

namespace Insequens.Application.Tests.Support;

public sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
}
