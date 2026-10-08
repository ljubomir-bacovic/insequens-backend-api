using MediatR;

namespace Insequens.Application.Commands.Tasks;

/// <summary>
/// Deletes every idempotency key past its retention. Meant to run on a schedule (INS-082); expired keys are already
/// ignored, so this only frees space. Returns how many keys were deleted.
/// </summary>
public record PurgeExpiredIdempotencyKeysCommand : IRequest<int>;
