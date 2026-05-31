namespace LinkUp.Domain.Enums;

public enum RecommendationType
{
    Friendship,
    Romance,
    Professional,
    Mentorship,
    Partnership
}

public enum RecommendationStatus
{
    Pending,
    PartiallyAccepted,
    Accepted,
    Rejected,
    Expired,
    Cancelled
}

public enum ConnectionRequestStatus
{
    Pending,
    Accepted,
    Rejected
}

public enum BlockType
{
    BlockByUser,
    DisableAll
}

public enum ContactType
{
    Phone,
    Email,
    Instagram,
    LinkedIn,
    WhatsApp,
    Other
}
