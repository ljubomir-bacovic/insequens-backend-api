namespace Insequens.Contracts.V1.Auth;

/// <param name="DeviceName">Optional label for this session, such as "iPhone", up to 100 characters.</param>
public sealed record LoginRequest(string Email, string Password, string? DeviceName = null);
