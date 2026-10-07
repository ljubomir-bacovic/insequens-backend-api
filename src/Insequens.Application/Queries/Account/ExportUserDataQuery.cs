using Insequens.Contracts.V1.Account;
using MediatR;

namespace Insequens.Application.Queries.Account;

public record ExportUserDataQuery(Guid UserId) : IRequest<UserDataExport>;
