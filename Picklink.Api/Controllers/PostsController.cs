using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Picklink.Application.Common;
using Picklink.Application.DTOs;
using Picklink.Application.Interfaces;
using Picklink.Domain.Entities;
using Picklink.Domain.Enums;
using Picklink.Infrastructure.Data;

namespace Picklink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PostsController(PicklinkDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PostResponse>>>> GetPosts(
        [FromQuery] PostQuery query,
        CancellationToken cancellationToken)
    {
        var posts = dbContext.Posts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == EntityStatus.Active)
            .AsQueryable();

        if (query.AuthorId.HasValue)
        {
            posts = posts.Where(x => x.AuthorId == query.AuthorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            posts = posts.Where(x => x.Content.Contains(query.Search));
        }

        var totalItems = await posts.CountAsync(cancellationToken);
        var items = await posts
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new PostResponse(
                x.Id,
                x.AuthorId,
                x.Content,
                x.Comments.Count(comment => comment.Status == EntityStatus.Active),
                x.Reactions.Count,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var meta = new { query.Page, query.PageSize, totalItems, totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize) };
        return Ok(ApiResponse<IReadOnlyList<PostResponse>>.Ok(items, meta: meta));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreatePost(CreatePostRequest request, CancellationToken cancellationToken)
    {
        var authorId = RequireUserId();
        var post = new Post
        {
            AuthorId = authorId,
            Content = request.Content
        };

        var sortOrder = 0;
        foreach (var mediaUrl in request.MediaUrls ?? [])
        {
            post.Media.Add(new PostMedia
            {
                Url = mediaUrl,
                MediaType = MediaType.Image,
                SortOrder = sortOrder++
            });
        }

        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(post.Id, "Post created."));
    }

    [Authorize]
    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateComment(Guid id, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var authorId = RequireUserId();
        var exists = await dbContext.Posts.AnyAsync(x => x.Id == id && x.Status == EntityStatus.Active, cancellationToken);
        if (!exists)
        {
            throw new AppException("Post not found.", 404);
        }

        var comment = new PostComment
        {
            PostId = id,
            AuthorId = authorId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content
        };

        dbContext.PostComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(comment.Id, "Comment created."));
    }

    [Authorize]
    [HttpPost("{id:guid}/reactions")]
    public async Task<ActionResult<ApiResponse>> ToggleReaction(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var postExists = await dbContext.Posts.AnyAsync(x => x.Id == id && x.Status == EntityStatus.Active, cancellationToken);
        if (!postExists)
        {
            throw new AppException("Post not found.", 404);
        }

        var reaction = await dbContext.PostReactions.FirstOrDefaultAsync(x => x.PostId == id && x.UserId == userId, cancellationToken);
        if (reaction is null)
        {
            dbContext.PostReactions.Add(new PostReaction { PostId = id, UserId = userId, Type = ReactionType.Like });
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse.Ok("Reaction added."));
        }

        dbContext.PostReactions.Remove(reaction);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Reaction removed."));
    }

    private Guid RequireUserId()
    {
        if (currentUserService.UserId is not { } userId)
        {
            throw new AppException("Unauthorized.", 401);
        }

        return userId;
    }
}
