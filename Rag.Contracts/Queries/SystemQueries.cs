using MediatR;
using Rag.Contracts.DTOs;

namespace Rag.Contracts.Queries;
public class GetSystemStatusQuery : IRequest<SystemStatusResponse>
{
}

public class GetProvidersQuery : IRequest<ProvidersResponse>
{
}
