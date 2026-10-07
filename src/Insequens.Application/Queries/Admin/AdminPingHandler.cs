using Insequens.Contracts.V1.Admin;
using MediatR;

namespace Insequens.Application.Queries.Admin;

public class AdminPingHandler(TimeProvider timeProvider) : IRequestHandler<AdminPingQuery, AdminPingResponse>
{
    public Task<AdminPingResponse> Handle(AdminPingQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(new AdminPingResponse("ok", timeProvider.GetUtcNow()));
}
