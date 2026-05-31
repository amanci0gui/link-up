using LinkUp.Domain.Enums;

namespace LinkUp.Domain.Entities;

public class ConnectionRequest
{
    public Guid Id { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid TargetId { get; private set; }
    public ConnectionRequestStatus Status { get; private set; }
    public string? Message { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ConnectionRequest() { }

    public static ConnectionRequest Create(Guid requesterId, Guid targetId, string? message)
    {
        return new ConnectionRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId,
            TargetId = targetId,
            Status = ConnectionRequestStatus.Pending,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Accept()
    {
        Status = ConnectionRequestStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = ConnectionRequestStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Reconstitutes a ConnectionRequest from persistence. Use only in repositories.</summary>
    public static ConnectionRequest Reconstitute(
        Guid id, Guid requesterId, Guid targetId,
        ConnectionRequestStatus status, string? message,
        DateTime createdAt, DateTime updatedAt)
    {
        return new ConnectionRequest
        {
            Id = id,
            RequesterId = requesterId,
            TargetId = targetId,
            Status = status,
            Message = message,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
