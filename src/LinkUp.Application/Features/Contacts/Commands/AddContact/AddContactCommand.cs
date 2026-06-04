using FluentValidation;
using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Contacts.Commands.AddContact;

public record AddContactCommand(
    ContactType Type,
    string Value,
    bool IsPublic) : IRequest<Result<AddContactResponse>>;

public record AddContactResponse(Guid ContactId, DateTime CreatedAt);

public class AddContactCommandValidator : AbstractValidator<AddContactCommand>
{
    public AddContactCommandValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Valor do contato é obrigatório.")
            .MaximumLength(255).WithMessage("Valor do contato deve ter no máximo 255 caracteres.");
    }
}

public class AddContactCommandHandler
    : IRequestHandler<AddContactCommand, Result<AddContactResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IContactRepository _contacts;

    public AddContactCommandHandler(ICurrentUserService currentUser, IContactRepository contacts)
    {
        _currentUser = currentUser;
        _contacts = contacts;
    }

    public async Task<Result<AddContactResponse>> Handle(
        AddContactCommand command, CancellationToken ct)
    {
        var contact = Contact.Create(_currentUser.UserId, command.Type, command.Value, command.IsPublic);
        await _contacts.AddAsync(contact, ct);
        return new AddContactResponse(contact.Id, contact.CreatedAt);
    }
}
