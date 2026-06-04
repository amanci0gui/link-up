using Dapper;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.Domain.Interfaces.Repositories;
using Npgsql;

namespace LinkUp.Infrastructure.Persistence.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly string _connectionString;

    public ContactRepository(string connectionString)
        => _connectionString = connectionString;

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<IReadOnlyList<Contact>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, user_id, type, value, is_public, created_at
            FROM contacts
            WHERE user_id = @UserId
            ORDER BY created_at ASC
            """;

        await using var conn = CreateConnection();
        var rows = await conn.QueryAsync<ContactRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public async Task AddAsync(Contact contact, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO contacts (id, user_id, type, value, is_public, created_at)
            VALUES (@Id, @UserId, @Type::contact_type, @Value, @IsPublic, @CreatedAt)
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            contact.Id,
            contact.UserId,
            Type = contact.Type.ToString().ToUpperInvariant(),
            contact.Value,
            contact.IsPublic,
            contact.CreatedAt
        }, cancellationToken: ct));
    }

    private sealed class ContactRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public bool IsPublic { get; init; }
        public DateTime CreatedAt { get; init; }

        public Contact ToDomain()
        {
            var type = Enum.Parse<ContactType>(Type, ignoreCase: true);
            return Contact.Reconstitute(Id, UserId, type, Value, IsPublic, CreatedAt);
        }
    }
}
