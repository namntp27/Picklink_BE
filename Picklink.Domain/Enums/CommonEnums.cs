namespace Picklink.Domain.Enums;

public enum EntityStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Archived = 4
}

public enum UserStatus
{
    Active = 1,
    Suspended = 2,
    Locked = 3
}

public enum Gender
{
    Unspecified = 0,
    Male = 1,
    Female = 2,
    Other = 3
}

public enum Visibility
{
    Public = 1,
    Private = 2,
    ClubOnly = 3
}

public enum ParticipantStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}

public enum VerificationStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3
}

public enum MediaType
{
    Image = 1,
    Video = 2,
    File = 3
}

public enum ReactionType
{
    Like = 1,
    Love = 2,
    Cheer = 3
}

public enum ReportTargetType
{
    Post = 1,
    Comment = 2,
    User = 3,
    Court = 4,
    Review = 5
}

public enum ReportStatus
{
    Pending = 1,
    Resolved = 2,
    Rejected = 3
}

public enum ConversationType
{
    Direct = 1,
    Group = 2,
    Club = 3
}

public enum MessageType
{
    Text = 1,
    Image = 2,
    File = 3
}
