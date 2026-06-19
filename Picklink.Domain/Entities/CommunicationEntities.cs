using Picklink.Domain.Common;
using Picklink.Domain.Enums;

namespace Picklink.Domain.Entities;

public sealed class Conversation : BaseEntity
{
    public ConversationType Type { get; set; } = ConversationType.Direct;
    public string? Title { get; set; }
    public ICollection<ConversationMember> Members { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}

public sealed class ConversationMember : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public Conversation? Conversation { get; set; }
}

public sealed class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? AttachmentUrl { get; set; }
    public Conversation? Conversation { get; set; }
}

public sealed class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
