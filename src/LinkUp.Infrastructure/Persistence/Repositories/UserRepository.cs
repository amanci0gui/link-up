using Dapper;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Interfaces.Repositories;
using Npgsql;

namespace LinkUp.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
        => _connectionString = connectionString;

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, email, password_hash, name, is_active, created_at, updated_at, deleted_at
            FROM users
            WHERE id = @Id AND deleted_at IS NULL
            """;

        await using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<UserRow>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, email, password_hash, name, is_active, created_at, updated_at, deleted_at
            FROM users
            WHERE email = @Email AND deleted_at IS NULL
            """;

        await using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<UserRow>(new CommandDefinition(sql, new { Email = email.ToLowerInvariant() }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE email = @Email AND deleted_at IS NULL";
        await using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Email = email.ToLowerInvariant() }, cancellationToken: ct));
        return count > 0;
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO users (id, email, password_hash, name, is_active, created_at, updated_at)
            VALUES (@Id, @Email, @PasswordHash, @Name, @IsActive, @CreatedAt, @UpdatedAt)
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            user.Id,
            user.Email,
            user.PasswordHash,
            user.Name,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        }, cancellationToken: ct));
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE users
            SET email = @Email, password_hash = @PasswordHash, name = @Name,
                is_active = @IsActive, updated_at = @UpdatedAt, deleted_at = @DeletedAt
            WHERE id = @Id
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            user.Id,
            user.Email,
            user.PasswordHash,
            user.Name,
            user.IsActive,
            user.UpdatedAt,
            user.DeletedAt
        }, cancellationToken: ct));
    }

    // Dapper mapping row
    private sealed class UserRow
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string PasswordHash { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? DeletedAt { get; init; }

        public User ToDomain()
        {
            // Use reflection-free mapping via factory method
            return User.Reconstitute(Id, Email, PasswordHash, Name, IsActive, CreatedAt, UpdatedAt, DeletedAt);
        }
    }
}
