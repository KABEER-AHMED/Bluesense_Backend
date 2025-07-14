using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.Models
{
    /// <summary>
    /// Represents a chat group that can be public or private
    /// </summary>
    public class Group
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// True for private groups (invite/approval only), False for public groups
        /// </summary>
        public bool IsPrivate { get; set; } = false;

        /// <summary>
        /// User who created the group
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Optional invite code for joining private groups
        /// </summary>
        [MaxLength(50)]
        public string? InviteCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public User? Creator { get; set; }
        public ICollection<GroupUser>? GroupUsers { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }
} 