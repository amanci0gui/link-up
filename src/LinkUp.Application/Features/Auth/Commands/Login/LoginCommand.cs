using FluentValidation;
using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;

public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IPasswordService _passwords;

    public LoginCommandHandler(
        IUserRepository users,
        ITokenService tokens,
        IPasswordService passwords)
    {
        _users = users;
        _tokens = tokens;
        _passwords = passwords;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);

        if (user is null || user.IsDeleted || !user.IsActive)
            return Errors.Auth.InvalidCredentials;

        if (!_passwords.Verify(request.Password, user.PasswordHash))
            return Errors.Auth.InvalidCredentials;

        var accessToken = _tokens.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _tokens.GenerateRefreshToken();
        var (_, tokenId) = _tokens.ParseRefreshToken(refreshToken);

        await _tokens.StoreRefreshTokenAsync(user.Id, tokenId, _passwords.Hash(refreshToken), ct);

        return new LoginResponse(accessToken, refreshToken, ExpiresIn: 900);
    }
}
