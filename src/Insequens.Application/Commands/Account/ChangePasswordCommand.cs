using MediatR;

namespace Insequens.Application.Commands.Account;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;
