namespace LinkUp.Domain.Entities;

public class Connection
{
    public Guid Id { get; private set; }
    public Guid UserId1 { get; private set; }
    public Guid UserId2 { get; private set; }
    public DateTime ConnectedAt { get; private set; }

    private Connection() { }

    public static Connection Create(Guid userId1, Guid userId2)
    {
        var (id1, id2) = Canonical(userId1, userId2);
        return new Connection
        {
            Id = Guid.NewGuid(),
            UserId1 = id1,
            UserId2 = id2,
            ConnectedAt = DateTime.UtcNow
        };
    }

    /// <summary>Reconstitutes a Connection from persistence. Use only in repositories.</summary>
    public static Connection Reconstitute(Guid id, Guid userId1, Guid userId2, DateTime connectedAt)
    {
        return new Connection
        {
            Id = id,
            UserId1 = userId1,
            UserId2 = userId2,
            ConnectedAt = connectedAt
        };
    }

    private static (Guid, Guid) Canonical(Guid a, Guid b) =>
        string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal) < 0
            ? (a, b)
            : (b, a);
}
