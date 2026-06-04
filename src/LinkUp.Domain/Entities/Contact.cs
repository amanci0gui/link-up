using LinkUp.Domain.Enums;

namespace LinkUp.Domain.Entities;

/// <summary>
/// Contato de um usuário (WhatsApp, Instagram, LinkedIn, etc.).
/// IsPublic=false indica que o contato só é compartilhado explicitamente
/// via ContactShare após indicação aceita.
/// </summary>
public class Contact
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public ContactType Type { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Contact() { }

    public static Contact Create(Guid userId, ContactType type, string value, bool isPublic = false)
    {
        return new Contact
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Value = value.Trim(),
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Reconstitui Contact do banco. Use apenas em repositórios.</summary>
    public static Contact Reconstitute(
        Guid id,
        Guid userId,
        ContactType type,
        string value,
        bool isPublic,
        DateTime createdAt)
    {
        return new Contact
        {
            Id = id,
            UserId = userId,
            Type = type,
            Value = value,
            IsPublic = isPublic,
            CreatedAt = createdAt
        };
    }
}
