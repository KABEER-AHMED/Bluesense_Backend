using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.Models
{
    /// <summary>
    /// Represents a chat message in a group
    /// </summary>
    public class Message
    {
        public Guid Id { get; set; }
        
        public Guid GroupId { get; set; }
        public Group? Group { get; set; }
        
        public Guid UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Message this is replying to (for threaded conversations)
        /// </summary>
        public Guid? ReplyToMessageId { get; set; }
        public Message? ReplyToMessage { get; set; }

        /// <summary>
        /// File attachments associated with this message
        /// </summary>
        public List<string> AttachmentUrls { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Soft delete flag - messages are never hard deleted for audit purposes
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Track if message has been edited
        /// </summary>
        public bool IsEdited { get; set; } = false;

        // Navigation properties for replies
        public ICollection<Message>? Replies { get; set; }
    }
} 