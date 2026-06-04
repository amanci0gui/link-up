using Dapper;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.Domain.Interfaces.Repositories;
using Npgsql;

namespace LinkUp.Infrastructure.Persistence.Repositories;

public class RecommendationRepository : IRecommendationRepository
{
    private readonly string _connectionString;

    public RecommendationRepository(string connectionString)
        => _connectionString = connectionString;

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Recommendation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, recommender_id, recommended_id, target_id,
                   type, status, message, created_at, updated_at, expires_at
            FROM recommendations
            WHERE id = @Id
            """;

        await using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<RecommendationRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Recommendation>> GetPendingByRecipientAsync(
        Guid userId, CancellationToken ct = default)
    {
        // Retorna inbox PENDING — usuário pode ser recommended_id OU target_id
        const string sql = """
            SELECT id, recommender_id, recommended_id, target_id,
                   type, status, message, created_at, updated_at, expires_at
            FROM recommendations
            WHERE (recommended_id = @UserId OR target_id = @UserId)
              AND status = 'PENDING'
            ORDER BY created_at DESC
            """;

        await using var conn = CreateConnection();
        var rows = await conn.QueryAsync<RecommendationRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<bool> ExistsPendingAsync(
        Guid recommenderId, Guid userA, Guid userB, CancellationToken ct = default)
    {
        // Ordenação canônica — verifica duplicata independente de direção do par
        var (id1, id2) = Canonical(userA, userB);
        const string sql = """
            SELECT COUNT(1)
            FROM recommendations
            WHERE recommender_id = @RecommenderId
              AND status = 'PENDING'
              AND (
                    (recommended_id = @Id1 AND target_id = @Id2)
                 OR (recommended_id = @Id2 AND target_id = @Id1)
              )
            """;

        await using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql,
                new { RecommenderId = recommenderId, Id1 = id1, Id2 = id2 },
                cancellationToken: ct));
        return count > 0;
    }

    public async Task AddAsync(Recommendation recommendation, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO recommendations
                (id, recommender_id, recommended_id, target_id,
                 type, status, message, created_at, updated_at, expires_at)
            VALUES
                (@Id, @RecommenderId, @RecommendedId, @TargetId,
                 @Type::recommendation_type, @Status::recommendation_status,
                 @Message, @CreatedAt, @UpdatedAt, @ExpiresAt)
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            recommendation.Id,
            recommendation.RecommenderId,
            recommendation.RecommendedId,
            recommendation.TargetId,
            Type = recommendation.Type.ToString().ToUpperInvariant(),
            Status = recommendation.Status.ToString().ToUpperInvariant(),
            recommendation.Message,
            recommendation.CreatedAt,
            recommendation.UpdatedAt,
            recommendation.ExpiresAt
        }, cancellationToken: ct));
    }

    public async Task UpdateAsync(Recommendation recommendation, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE recommendations
            SET status     = @Status::recommendation_status,
                updated_at = @UpdatedAt
            WHERE id = @Id
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            recommendation.Id,
            Status = recommendation.Status.ToString().ToUpperInvariant(),
            recommendation.UpdatedAt
        }, cancellationToken: ct));
    }

    // Mesmo helper de canonical ordering usado por ConnectionRepository
    private static (Guid, Guid) Canonical(Guid a, Guid b) =>
        string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal) < 0
            ? (a, b)
            : (b, a);

    private sealed class RecommendationRow
    {
        public Guid Id { get; init; }
        public Guid RecommenderId { get; init; }
        public Guid RecommendedId { get; init; }
        public Guid TargetId { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string? Message { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime ExpiresAt { get; init; }

        public Recommendation ToDomain()
        {
            var type = Enum.Parse<RecommendationType>(Type, ignoreCase: true);
            var status = Enum.Parse<RecommendationStatus>(Status, ignoreCase: true);
            return Recommendation.Reconstitute(
                Id, RecommenderId, RecommendedId, TargetId,
                type, status, Message, CreatedAt, UpdatedAt, ExpiresAt);
        }
    }
}
