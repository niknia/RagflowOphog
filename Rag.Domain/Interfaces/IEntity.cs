namespace Rag.Domain.Interfaces;
public interface IEntity
{
    Guid Id { get; set; }
    DateTime CreatedAt { get; set; }
}
