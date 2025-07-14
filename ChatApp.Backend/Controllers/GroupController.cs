using ChatApp.Backend.DTOs;
using ChatApp.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Backend.Controllers
{
    /// <summary>
    /// Controller for group management operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ILogger<GroupController> _logger;

        public GroupController(IGroupService groupService, ILogger<GroupController> logger)
        {
            _groupService = groupService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new group
        /// </summary>
        /// <param name="createGroupDto">Group creation details</param>
        /// <returns>Created group information</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<GroupDto>>> CreateGroup([FromBody] CreateGroupDto createGroupDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.CreateGroupAsync(createGroupDto, userId);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                return StatusCode(500, ApiResponse<GroupDto>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Update group details
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <param name="updateGroupDto">Group update details</param>
        /// <returns>Updated group information</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<GroupDto>>> UpdateGroup(Guid id, [FromBody] UpdateGroupDto updateGroupDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.UpdateGroupAsync(id, updateGroupDto, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("permissions"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group {GroupId}", id);
                return StatusCode(500, ApiResponse<GroupDto>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Delete a group
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteGroup(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.DeleteGroupAsync(id, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("permissions") || result.Message.Contains("admin"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group {GroupId}", id);
                return StatusCode(500, ApiResponse<bool>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Get group details
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <returns>Group information</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<GroupDto>>> GetGroup(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.GetGroupAsync(id, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("denied"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group {GroupId}", id);
                return StatusCode(500, ApiResponse<GroupDto>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Get user's groups
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <returns>Paginated list of user's groups</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<GroupDto>>>> GetUserGroups(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.GetUserGroupsAsync(userId, page, pageSize);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user groups");
                return StatusCode(500, ApiResponse<PagedResult<GroupDto>>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Search public groups
        /// </summary>
        /// <param name="searchTerm">Search term (optional)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <returns>Paginated list of public groups</returns>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResult<GroupDto>>>> SearchPublicGroups(
            [FromQuery] string? searchTerm,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _groupService.SearchPublicGroupsAsync(searchTerm, page, pageSize);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching public groups");
                return StatusCode(500, ApiResponse<PagedResult<GroupDto>>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Join a group
        /// </summary>
        /// <param name="joinGroupDto">Join group details</param>
        /// <returns>Join result</returns>
        [HttpPost("join")]
        public async Task<ActionResult<ApiResponse<bool>>> JoinGroup([FromBody] JoinGroupDto joinGroupDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.JoinGroupAsync(joinGroupDto, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining group");
                return StatusCode(500, ApiResponse<bool>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Leave a group
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <returns>Leave result</returns>
        [HttpPost("{id}/leave")]
        public async Task<ActionResult<ApiResponse<bool>>> LeaveGroup(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.LeaveGroupAsync(id, userId);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving group {GroupId}", id);
                return StatusCode(500, ApiResponse<bool>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Get group members
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <returns>Paginated list of group members</returns>
        [HttpGet("{id}/members")]
        public async Task<ActionResult<ApiResponse<PagedResult<GroupMemberDto>>>> GetGroupMembers(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.GetGroupMembersAsync(id, userId, page, pageSize);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("denied"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members for group {GroupId}", id);
                return StatusCode(500, ApiResponse<PagedResult<GroupMemberDto>>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Remove a member from group
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <param name="memberId">Member user ID to remove</param>
        /// <returns>Removal result</returns>
        [HttpDelete("{id}/members/{memberId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveMember(Guid id, Guid memberId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.RemoveMemberAsync(id, memberId, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("permissions") || result.Message.Contains("cannot"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member {MemberId} from group {GroupId}", memberId, id);
                return StatusCode(500, ApiResponse<bool>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Update member role
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <param name="memberId">Member user ID</param>
        /// <param name="updateRoleDto">Role update details</param>
        /// <returns>Update result</returns>
        [HttpPut("{id}/members/{memberId}/role")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateMemberRole(
            Guid id, 
            Guid memberId, 
            [FromBody] UpdateMemberRoleDto updateRoleDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.UpdateMemberRoleAsync(id, memberId, updateRoleDto.Role, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("admin") || result.Message.Contains("creator"))
                        return Forbid();
                    if (result.Message.Contains("Invalid"))
                        return BadRequest(result);
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for member {MemberId} in group {GroupId}", memberId, id);
                return StatusCode(500, ApiResponse<bool>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Generate invite code for private group
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <returns>Generated invite code</returns>
        [HttpPost("{id}/invite-code")]
        public async Task<ActionResult<ApiResponse<string>>> GenerateInviteCode(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _groupService.GenerateInviteCodeAsync(id, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("permissions"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invite code for group {GroupId}", id);
                return StatusCode(500, ApiResponse<string>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Extract user ID from JWT claims
        /// </summary>
        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return userId;
        }
    }
}
