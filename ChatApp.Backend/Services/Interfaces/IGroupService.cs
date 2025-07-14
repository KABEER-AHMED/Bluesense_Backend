using ChatApp.Backend.DTOs;

namespace ChatApp.Backend.Services.Interfaces
{
    /// <summary>
    /// Interface for group management operations
    /// </summary>
    public interface IGroupService
    {
        /// <summary>
        /// Create a new group
        /// </summary>
        Task<ApiResponse<GroupDto>> CreateGroupAsync(CreateGroupDto createGroupDto, Guid userId);

        /// <summary>
        /// Update group details
        /// </summary>
        Task<ApiResponse<GroupDto>> UpdateGroupAsync(Guid groupId, UpdateGroupDto updateGroupDto, Guid userId);

        /// <summary>
        /// Delete a group (soft delete)
        /// </summary>
        Task<ApiResponse<bool>> DeleteGroupAsync(Guid groupId, Guid userId);

        /// <summary>
        /// Get group details
        /// </summary>
        Task<ApiResponse<GroupDto>> GetGroupAsync(Guid groupId, Guid userId);

        /// <summary>
        /// Get all groups for a user
        /// </summary>
        Task<ApiResponse<PagedResult<GroupDto>>> GetUserGroupsAsync(Guid userId, int page = 1, int pageSize = 20);

        /// <summary>
        /// Search public groups
        /// </summary>
        Task<ApiResponse<PagedResult<GroupDto>>> SearchPublicGroupsAsync(string? searchTerm, int page = 1, int pageSize = 20);

        /// <summary>
        /// Join a group
        /// </summary>
        Task<ApiResponse<bool>> JoinGroupAsync(JoinGroupDto joinGroupDto, Guid userId);

        /// <summary>
        /// Leave a group
        /// </summary>
        Task<ApiResponse<bool>> LeaveGroupAsync(Guid groupId, Guid userId);

        /// <summary>
        /// Get group members
        /// </summary>
        Task<ApiResponse<PagedResult<GroupMemberDto>>> GetGroupMembersAsync(Guid groupId, Guid userId, int page = 1, int pageSize = 50);

        /// <summary>
        /// Remove a member from group (admin/moderator only)
        /// </summary>
        Task<ApiResponse<bool>> RemoveMemberAsync(Guid groupId, Guid memberUserId, Guid requestingUserId);

        /// <summary>
        /// Update member role (admin only)
        /// </summary>
        Task<ApiResponse<bool>> UpdateMemberRoleAsync(Guid groupId, Guid memberUserId, string role, Guid requestingUserId);

        /// <summary>
        /// Generate invite code for private group
        /// </summary>
        Task<ApiResponse<string>> GenerateInviteCodeAsync(Guid groupId, Guid userId);
    }
}
