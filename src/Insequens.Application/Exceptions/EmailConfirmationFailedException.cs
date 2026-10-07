namespace Insequens.Application.Exceptions;

public sealed class EmailConfirmationFailedException : Exception
{
    public EmailConfirmationFailedException()
        : base("Email confirmation failed.")
    {
    }
}
