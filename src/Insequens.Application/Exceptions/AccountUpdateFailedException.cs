namespace Insequens.Application.Exceptions;

/// <summary>
/// An account change by its signed-in owner was refused (400). The reason is safe to show: the caller already
/// controls the account, so it reveals nothing about other accounts.
/// </summary>
public sealed class AccountUpdateFailedException(string reason) : Exception(reason);
