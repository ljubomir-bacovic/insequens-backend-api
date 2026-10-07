namespace Insequens.Application.Abstractions;

/// <summary>The authenticated caller, if any. Background work has none.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}
