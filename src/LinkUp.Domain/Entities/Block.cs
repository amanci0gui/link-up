using LinkUp.Domain.Enums;

namespace LinkUp.Domain.Entities;

public class Block
{
    public Guid Id { get; private set; }
    public Guid BlockerId { get; private set; }
    public Guid BlockedId { get; private set; }
    public BlockType BlockType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Block() { }

    public static Block Create(Guid blockerId, Guid blockedId, BlockType type = BlockType.BlockByUser)
    {
        return new Block
        {
            Id = Guid.NewGuid(),
            BlockerId = blockerId,
            BlockedId = blockedId,
            BlockType = type,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Reconstitutes a Block from persistence. Use only in repositories.</summary>
    public static Block Reconstitute(Guid id, Guid blockerId, Guid blockedId, BlockType blockType, DateTime createdAt)
    {
        return new Block
        {
            Id = id,
            BlockerId = blockerId,
            BlockedId = blockedId,
            BlockType = blockType,
            CreatedAt = createdAt
        };
    }
}
