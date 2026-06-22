using MediatR;
using Rag.Contracts.DTOs;

namespace Rag.Contracts.Queries;
public class GetChatHistoryQuery : IRequest<ChatDto?>
{
    public Guid Id { get; set; }
}
