namespace Insequens.Contracts.V1.Admin;

public sealed record AdminPingResponse(string Status, DateTimeOffset ServerTime);
