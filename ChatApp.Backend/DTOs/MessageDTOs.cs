using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.DTOs
{
    /// <summary>
    /// DTO for sending a new message
    /// </summary>
    public class SendMessageDto
    {
        [Required]
        public Guid GroupId { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        public Guid? ReplyToMessageId { get; set; }

        public List<string>? AttachmentUrls { get; set; }
    }

    /// <summary>
    /// DTO for updating/editing a message
    /// </summary>
    public class UpdateMessageDto
    {
        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for message information
    /// </summary>
    public class MessageDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public MessageDto? ReplyToMessage { get; set; }
        public List<string> AttachmentUrls { get; set; } = new();
    }

    /// <summary>
    /// DTO for paginated message results
    /// </summary>
    public class MessagePageDto
    {
        public List<MessageDto> Messages { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// DTO for message search parameters
    /// </summary>
    public class MessageSearchDto
    {
        public Guid GroupId { get; set; }
        public string? SearchTerm { get; set; }
        public Guid? FromUserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
