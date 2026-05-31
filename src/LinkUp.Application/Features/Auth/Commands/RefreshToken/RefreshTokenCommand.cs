using FluentValidation;
using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using MediatR;

namespace LinkUp.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshTokenResponse>>;

public record RefreshTokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly ITokenService _tokens;
    private readonly IPasswordService _passwords;

    public RefreshTokenCommandHandler(ITokenService tokens, IPasswordService passwords)
    {
        _tokens = tokens;
        _passwords = passwords;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        try
        {
            var (userId, tokenId) = _tokens.ParseRefreshToken(request.RefreshToken);

            var isValid = await _tokens.ValidateRefreshTokenAsync(userId, tokenId, request.RefreshToken, ct);
            if (!isValid)
                return Errors.Auth.InvalidRefreshToken;

            await _tokens.RevokeRefreshTokenAsync(userId, tokenId, ct);

            var newAccessToken = _tokens.GenerateAccessToken(userId, string.Empty);
            var newRefreshToken = _tokens.GenerateRefreshToken();
            var (_, newTokenId) = _tokens.ParseRefreshToken(newRefreshToken);

            await _tokens.StoreRefreshTokenAsync(
                userId, newTokenId, _passwords.Hash(newRefreshToken), ct);

            return new RefreshTokenResponse(newAccessToken, newRefreshToken, ExpiresIn: 900);
        }
        catch
        {
            return Errors.Auth.InvalidRefreshToken;
        }
    }
}
