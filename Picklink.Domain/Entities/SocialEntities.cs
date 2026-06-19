using Picklink.Domain.Common;
using Picklink.Domain.Enums;

namespace Picklink.Domain.Entities;

public sealed class Post : BaseEntity
{
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Visibility Visibility { get; set; } = Visibility.Public;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public ICollection<PostMedia> Media { get; set; } = [];
    public ICollection<PostComment> Comments { get; set; } = [];
    public ICollection<PostReaction> Reactions { get; set; } = [];
}

public sealed class PostMedia : BaseEntity
{
    public Guid PostId { get; set; }
    public string Url { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Image;
    public int SortOrder { get; set; }
    public Post? Post { get; set; }
}

public sealed class PostComment : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public Post? Post { get; set; }
}

public sealed class PostReaction : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public ReactionType Type { get; set; } = ReactionType.Like;
    public Post? Post { get; set; }
}

public sealed class SavedPost : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public Post? Post { get; set; }
}

public sealed class Report : BaseEntity
{
    public Guid ReporterId { get; set; }
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
}
