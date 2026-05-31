using FluentValidation;
using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Profile.Commands.UpdateProfile;

public record UpdateProfileCommand(string Name, string? Bio, string? ProfilePictureUrl) : IRequest<Result<UpdateProfileResponse>>;

public record UpdateProfileResponse(Guid Id, string Name, string? Bio, string? ProfilePictureUrl);

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter ao menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio deve ter no máximo 500 caracteres.")
            .When(x => x.Bio is not null);

        RuleFor(x => x.ProfilePictureUrl)
            .MaximumLength(500).WithMessage("URL da foto deve ter no máximo 500 caracteres.")
            .When(x => x.ProfilePictureUrl is not null);
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _users;

    public UpdateProfileCommandHandler(ICurrentUserService currentUser, IUserRepository users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<UpdateProfileResponse>> Handle(UpdateProfileCommand command, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId, ct);
        if (user is null || !user.IsActive || user.IsDeleted)
            return Errors.User.NotFound;

        user.UpdateProfile(command.Name, command.Bio, command.ProfilePictureUrl);
        await _users.UpdateAsync(user, ct);

        return new UpdateProfileResponse(user.Id, user.Name, user.Bio, user.ProfilePictureUrl);
    }
}
