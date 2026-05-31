namespace LinkUp.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Bio { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    private User() { }

    public static User Create(string email, string passwordHash, string name)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Reconstitutes a User from persistence. Use only in repositories.</summary>
    public static User Reconstitute(
        Guid id, string email, string passwordHash, string name,
        string? bio, string? profilePictureUrl,
        bool isActive, DateTime createdAt, DateTime updatedAt, DateTime? deletedAt)
    {
        return new User
        {
            Id = id,
            Email = email,
            PasswordHash = passwordHash,
            Name = name,
            Bio = bio,
            ProfilePictureUrl = profilePictureUrl,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt
        };
    }

    public void UpdateProfile(string name, string? bio, string? profilePictureUrl)
    {
        Name = name.Trim();
        Bio = bio;
        ProfilePictureUrl = profilePictureUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Anonymize()
    {
        Email = $"deleted_{Id}@anonymized.linkup";
        Name = "Usuário Removido";
        Bio = null;
        ProfilePictureUrl = null;
        PasswordHash = string.Empty;
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
