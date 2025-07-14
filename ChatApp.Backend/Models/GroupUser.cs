using System;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.Models
{
    /// <summary>
    /// Represents the many-to-many relationship between users and groups
    /// </summary>
    public class GroupUser
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }
        
        public Guid GroupId { get; set; }
        public Group? Group { get; set; }

        /// <summary>
        /// Role in the group: Admin, Moderator, Member
        /// </summary>
        [MaxLength(20)]
        public string Role { get; set; } = "Member";

        /// <summary>
        /// For private groups, whether the join request has been approved
        /// </summary>
        public bool IsApproved { get; set; } = true; // Auto-approved for public groups

        /// <summary>
        /// Whether user is banned from the group
        /// </summary>
        public bool IsBanned { get; set; } = false;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LeftAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
} 