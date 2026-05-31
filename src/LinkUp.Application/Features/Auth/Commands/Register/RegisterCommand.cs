using FluentValidation;
using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Name, string Email, string Password) : IRequest<Result<RegisterResponse>>;

public record RegisterResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter ao menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter ao menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("Senha deve conter ao menos uma letra maiúscula.")
            .Matches("[0-9]").WithMessage("Senha deve conter ao menos um número.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IPasswordService _passwords;

    public RegisterCommandHandler(
        IUserRepository users,
        ITokenService tokens,
        IPasswordService passwords)
    {
        _users = users;
        _tokens = tokens;
        _passwords = passwords;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        if (await _users.ExistsByEmailAsync(request.Email, ct))
            return Errors.Auth.EmailAlreadyExists;

        var passwordHash = _passwords.Hash(request.Password);
        var user = User.Create(request.Email, passwordHash, request.Name);

        await _users.AddAsync(user, ct);

        var accessToken = _tokens.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _tokens.GenerateRefreshToken();
        var (_, tokenId) = _tokens.ParseRefreshToken(refreshToken);

        await _tokens.StoreRefreshTokenAsync(user.Id, tokenId, _passwords.Hash(refreshToken), ct);

        return new RegisterResponse(accessToken, refreshToken, ExpiresIn: 900);
    }
}
