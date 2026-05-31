namespace LinkUp.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string UserEmail { get; }
}

public interface IDateTimeService
{
    DateTime UtcNow { get; }
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default);
}

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email);
    string GenerateRefreshToken();
    Task StoreRefreshTokenAsync(Guid userId, string tokenId, string tokenHash, CancellationToken ct = default);
    Task<bool> ValidateRefreshTokenAsync(Guid userId, string tokenId, string token, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(Guid userId, string tokenId, CancellationToken ct = default);
    (Guid userId, string tokenId) ParseRefreshToken(string refreshToken);
}
