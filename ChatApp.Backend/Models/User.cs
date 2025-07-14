using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.Models
{
    /// <summary>
    /// Represents a user in the chat application
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User's display name (can be different from username)
        /// </summary>
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Profile picture URL
        /// </summary>
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// User's status: Online, Away, Busy, Offline
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Offline";

        /// <summary>
        /// Last time user was active
        /// </summary>
        public DateTime? LastActiveAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<GroupUser>? GroupUsers { get; set; }
        public ICollection<Message>? Messages { get; set; }
        public ICollection<RefreshToken>? RefreshTokens { get; set; }
        public ICollection<Group>? CreatedGroups { get; set; }
    }
} 