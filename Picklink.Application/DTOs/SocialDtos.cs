using Picklink.Application.Common;

namespace Picklink.Application.DTOs;

public sealed class PostQuery : PagedQuery
{
    public Guid? AuthorId { get; set; }
}

public sealed record CreatePostRequest(string Content, IReadOnlyList<string>? MediaUrls);

public sealed record PostResponse(
    Guid Id,
    Guid AuthorId,
    string Content,
    int CommentCount,
    int ReactionCount,
    DateTimeOffset CreatedAt);

public sealed record CreateCommentRequest(string Content, Guid? ParentCommentId);
