using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

/// <param name="DeviceName">Optional label the client chooses for this session, such as "iPhone".</param>
/// <param name="IpAddress">The client address, recorded on the refresh token; never logged.</param>
public record LoginCommand(string Email, string Password, string? DeviceName = null, string? IpAddress = null)
    : IRequest<AuthTokensResponse>;
