using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Account;

public static class AccountResponses
{
    /// <summary>The same body whether or not the new address can be used, so it cannot be used to find accounts.</summary>
    public static AuthMessageResponse EmailChangeRequested { get; } =
        new("If the new address can be used, a confirmation email has been sent to it.");

    public static AuthMessageResponse EmailChanged { get; } = new("Email address changed. Please sign in again.");
}
