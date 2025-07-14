using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.DTOs
{
    /// <summary>
    /// DTO for creating a new group
    /// </summary>
    public class CreateGroupDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsPrivate { get; set; } = false;
    }

    /// <summary>
    /// DTO for updating group details
    /// </summary>
    public class UpdateGroupDto
    {
        [StringLength(100, MinimumLength = 1)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool? IsPrivate { get; set; }
    }

    /// <summary>
    /// DTO for group information
    /// </summary>
    public class GroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public List<GroupMemberDto> Members { get; set; } = new();
    }

    /// <summary>
    /// DTO for group member information
    /// </summary>
    public class GroupMemberDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    /// <summary>
    /// DTO for joining a group
    /// </summary>
    public class JoinGroupDto
    {
        public Guid? GroupId { get; set; }
        
        public string? InviteCode { get; set; }
    }

    /// <summary>
    /// DTO for updating member role
    /// </summary>
    public class UpdateMemberRoleDto
    {
        [Required]
        [RegularExpression("^(Admin|Moderator|Member)$", ErrorMessage = "Role must be Admin, Moderator, or Member")]
        public string Role { get; set; } = string.Empty;
    }
}
