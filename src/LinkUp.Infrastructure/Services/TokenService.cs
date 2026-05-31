using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LinkUp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace LinkUp.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly IConnectionMultiplexer _redis;

    public TokenService(IConfiguration config, IConnectionMultiplexer redis)
    {
        _config = config;
        _redis = redis;
    }

    public string GenerateAccessToken(Guid userId, string email)
    {
        var secret = _config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expMinutes = int.Parse(_config["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var tokenId = Guid.NewGuid().ToString();
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        // Encode userId:tokenId:secret as base64 payload — real impl signs this
        var payload = JsonSerializer.Serialize(new { tokenId, secret });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    public (Guid userId, string tokenId) ParseRefreshToken(string refreshToken)
    {
        // For MVP: tokenId embedded in token; userId retrieved from Redis
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(refreshToken));
        var data = JsonSerializer.Deserialize<RefreshPayload>(json)!;
        // userId is not stored in token for security — must match via Redis lookup
        return (Guid.Empty, data.TokenId);
    }

    public async Task StoreRefreshTokenAsync(Guid userId, string tokenId, string tokenHash, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"rt:{userId}:{tokenId}";
        var expDays = int.Parse(_config["Jwt:RefreshTokenExpirationDays"] ?? "30");
        await db.StringSetAsync(key, tokenHash, TimeSpan.FromDays(expDays));
    }

    public async Task<bool> ValidateRefreshTokenAsync(Guid userId, string tokenId, string token, CancellationToken ct = default)
    {
        // Scan Redis for rt:{userId}:{tokenId} — since we store userId separately
        // For MVP: client sends userId in refresh request body OR we embed it in token
        // Simplified: search by pattern rt:{*}:{tokenId}
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var pattern = $"rt:*:{tokenId}";
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            var stored = await db.StringGetAsync(key);
            if (!stored.HasValue) continue;
            return BCrypt.Net.BCrypt.Verify(token, stored!);
        }
        return false;
    }

    public async Task RevokeRefreshTokenAsync(Guid userId, string tokenId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var pattern = $"rt:*:{tokenId}";
        await foreach (var key in server.KeysAsync(pattern: pattern))
            await db.KeyDeleteAsync(key);
    }

    private sealed record RefreshPayload(string TokenId, string Secret)
    {
        [System.Text.Json.Serialization.JsonConstructor]
        public RefreshPayload() : this(string.Empty, string.Empty) { }
    }
}
