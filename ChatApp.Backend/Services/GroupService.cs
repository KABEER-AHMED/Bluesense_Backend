using ChatApp.Backend.DTOs;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace ChatApp.Backend.Services
{
    /// <summary>
    /// Service for group management operations
    /// </summary>
    public class GroupService : IGroupService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<GroupService> _logger;

        public GroupService(AppDbContext context, IMapper mapper, ILogger<GroupService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Create a new group
        /// </summary>
        public async Task<ApiResponse<GroupDto>> CreateGroupAsync(CreateGroupDto createGroupDto, Guid userId)
        {
            try
            {
                // Validate user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ApiResponse<GroupDto>.Failure("User not found", 404);
                }

                var group = new Group
                {
                    Id = Guid.NewGuid(),
                    Name = createGroupDto.Name,
                    Description = createGroupDto.Description,
                    IsPrivate = createGroupDto.IsPrivate,
                    CreatedBy = userId,
                    InviteCode = createGroupDto.IsPrivate ? GenerateInviteCode() : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Groups.Add(group);

                // Add creator as admin
                var groupUser = new GroupUser
                {
                    UserId = userId,
                    GroupId = group.Id,
                    Role = "Admin",
                    IsApproved = true,
                    JoinedAt = DateTime.UtcNow
                };

                _context.GroupUsers.Add(groupUser);

                await _context.SaveChangesAsync();

                var groupDto = _mapper.Map<GroupDto>(group);
                
                _logger.LogInformation("Group {GroupId} created by user {UserId}", group.Id, userId);
                
                return ApiResponse<GroupDto>.Success(groupDto, "Group created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group for user {UserId}", userId);
                return ApiResponse<GroupDto>.Failure("Failed to create group");
            }
        }

        /// <summary>
        /// Update group details
        /// </summary>
        public async Task<ApiResponse<GroupDto>> UpdateGroupAsync(Guid groupId, UpdateGroupDto updateGroupDto, Guid userId)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.GroupUsers)
                    .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

                if (group == null)
                {
                    return ApiResponse<GroupDto>.Failure("Group not found", 404);
                }

                // Check if user is admin or moderator
                var userRole = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId)?.Role;
                if (userRole != "Admin" && userRole != "Moderator")
                {
                    return ApiResponse<GroupDto>.Failure("Insufficient permissions", 403);
                }

                // Update group properties
                if (!string.IsNullOrEmpty(updateGroupDto.Name))
                    group.Name = updateGroupDto.Name;

                if (updateGroupDto.Description != null)
                    group.Description = updateGroupDto.Description;

                if (updateGroupDto.IsPrivate.HasValue)
                {
                    group.IsPrivate = updateGroupDto.IsPrivate.Value;
                    if (group.IsPrivate && string.IsNullOrEmpty(group.InviteCode))
                    {
                        group.InviteCode = GenerateInviteCode();
                    }
                    else if (!group.IsPrivate)
                    {
                        group.InviteCode = null;
                    }
                }

                group.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var groupDto = _mapper.Map<GroupDto>(group);
                
                _logger.LogInformation("Group {GroupId} updated by user {UserId}", groupId, userId);
                
                return ApiResponse<GroupDto>.Success(groupDto, "Group updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group {GroupId} by user {UserId}", groupId, userId);
                return ApiResponse<GroupDto>.Failure("Failed to update group");
            }
        }

        /// <summary>
        /// Delete a group (soft delete)
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteGroupAsync(Guid groupId, Guid userId)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.GroupUsers)
                    .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

                if (group == null)
                {
                    return ApiResponse<bool>.Failure("Group not found", 404);
                }

                // Only admin can delete group
                var userRole = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId)?.Role;
                if (userRole != "Admin")
                {
                    return ApiResponse<bool>.Failure("Only admins can delete groups", 403);
                }

                group.IsDeleted = true;
                group.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Group {GroupId} deleted by user {UserId}", groupId, userId);
                
                return ApiResponse<bool>.Success(true, "Group deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group {GroupId} by user {UserId}", groupId, userId);
                return ApiResponse<bool>.Failure("Failed to delete group");
            }
        }

        /// <summary>
        /// Get group details
        /// </summary>
        public async Task<ApiResponse<GroupDto>> GetGroupAsync(Guid groupId, Guid userId)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.GroupUsers.Where(gu => !gu.IsDeleted))
                    .ThenInclude(gu => gu.User)
                    .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

                if (group == null)
                {
                    return ApiResponse<GroupDto>.Failure("Group not found", 404);
                }

                // Check if user is a member or if group is public
                var isMember = group.GroupUsers.Any(gu => gu.UserId == userId && gu.IsApproved);
                if (!isMember && group.IsPrivate)
                {
                    return ApiResponse<GroupDto>.Failure("Access denied", 403);
                }

                var groupDto = _mapper.Map<GroupDto>(group);
                
                return ApiResponse<GroupDto>.Success(groupDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group {GroupId} for user {UserId}", groupId, userId);
                return ApiResponse<GroupDto>.Failure("Failed to get group");
            }
        }

        /// <summary>
        /// Get all groups for a user
        /// </summary>
        public async Task<ApiResponse<PagedResult<GroupDto>>> GetUserGroupsAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var skip = (page - 1) * pageSize;

                var groupsQuery = _context.Groups
                    .Include(g => g.GroupUsers.Where(gu => !gu.IsDeleted))
                    .ThenInclude(gu => gu.User)
                    .Where(g => !g.IsDeleted && 
                                g.GroupUsers.Any(gu => gu.UserId == userId && gu.IsApproved && !gu.IsDeleted))
                    .OrderByDescending(g => g.UpdatedAt);

                var totalCount = await groupsQuery.CountAsync();
                var groups = await groupsQuery
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var groupDtos = _mapper.Map<List<GroupDto>>(groups);

                var pagedResult = new PagedResult<GroupDto>
                {
                    Items = groupDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return ApiResponse<PagedResult<GroupDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups for user {UserId}", userId);
                return ApiResponse<PagedResult<GroupDto>>.Failure("Failed to get user groups");
            }
        }

        /// <summary>
        /// Search public groups
        /// </summary>
        public async Task<ApiResponse<PagedResult<GroupDto>>> SearchPublicGroupsAsync(string? searchTerm, int page = 1, int pageSize = 20)
        {
            try
            {
                var skip = (page - 1) * pageSize;

                var groupsQuery = _context.Groups
                    .Include(g => g.GroupUsers.Where(gu => !gu.IsDeleted))
                    .ThenInclude(gu => gu.User)
                    .Where(g => !g.IsDeleted && !g.IsPrivate);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    groupsQuery = groupsQuery.Where(g => 
                        g.Name.Contains(searchTerm) || 
                        (g.Description != null && g.Description.Contains(searchTerm)));
                }

                groupsQuery = groupsQuery.OrderByDescending(g => g.CreatedAt);

                var totalCount = await groupsQuery.CountAsync();
                var groups = await groupsQuery
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var groupDtos = _mapper.Map<List<GroupDto>>(groups);

                var pagedResult = new PagedResult<GroupDto>
                {
                    Items = groupDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return ApiResponse<PagedResult<GroupDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching public groups with term: {SearchTerm}", searchTerm);
                return ApiResponse<PagedResult<GroupDto>>.Failure("Failed to search groups");
            }
        }

        /// <summary>
        /// Join a group
        /// </summary>
        public async Task<ApiResponse<bool>> JoinGroupAsync(JoinGroupDto joinGroupDto, Guid userId)
        {
            try
            {
                Group? group = null;

                // Find group by ID or invite code
                if (joinGroupDto.GroupId.HasValue)
                {
                    group = await _context.Groups
                        .Include(g => g.GroupUsers)
                        .FirstOrDefaultAsync(g => g.Id == joinGroupDto.GroupId && !g.IsDeleted);
                }
                else if (!string.IsNullOrEmpty(joinGroupDto.InviteCode))
                {
                    group = await _context.Groups
                        .Include(g => g.GroupUsers)
                        .FirstOrDefaultAsync(g => g.InviteCode == joinGroupDto.InviteCode && !g.IsDeleted);
                }

                if (group == null)
                {
                    return ApiResponse<bool>.Failure("Group not found", 404);
                }

                // Check if user is already a member
                var existingMembership = group.GroupUsers
                    .FirstOrDefault(gu => gu.UserId == userId && !gu.IsDeleted);

                if (existingMembership != null)
                {
                    if (existingMembership.IsApproved)
                    {
                        return ApiResponse<bool>.Failure("Already a member of this group", 400);
                    }
                    else
                    {
                        return ApiResponse<bool>.Failure("Join request already pending approval", 400);
                    }
                }

                // For private groups, approval may be required
                var isApproved = !group.IsPrivate;

                var groupUser = new GroupUser
                {
                    UserId = userId,
                    GroupId = group.Id,
                    Role = "Member",
                    IsApproved = isApproved,
                    JoinedAt = DateTime.UtcNow
                };

                _context.GroupUsers.Add(groupUser);
                await _context.SaveChangesAsync();

                var message = isApproved ? "Successfully joined group" : "Join request sent for approval";
                
                _logger.LogInformation("User {UserId} joined group {GroupId} (Approved: {IsApproved})", 
                    userId, group.Id, isApproved);
                
                return ApiResponse<bool>.Success(true, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining group for user {UserId}", userId);
                return ApiResponse<bool>.Failure("Failed to join group");
            }
        }

        /// <summary>
        /// Leave a group
        /// </summary>
        public async Task<ApiResponse<bool>> LeaveGroupAsync(Guid groupId, Guid userId)
        {
            try
            {
                var groupUser = await _context.GroupUsers
                    .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == userId && !gu.IsDeleted);

                if (groupUser == null)
                {
                    return ApiResponse<bool>.Failure("You are not a member of this group", 400);
                }

                groupUser.IsDeleted = true;
                groupUser.LeftAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);
                
                return ApiResponse<bool>.Success(true, "Successfully left group");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving group {GroupId} for user {UserId}", groupId, userId);
                return ApiResponse<bool>.Failure("Failed to leave group");
            }
        }

        /// <summary>
        /// Get group members
        /// </summary>
        public async Task<ApiResponse<PagedResult<GroupMemberDto>>> GetGroupMembersAsync(Guid groupId, Guid userId, int page = 1, int pageSize = 50)
        {
            try
            {
                // Check if user is a member of the group
                var userMembership = await _context.GroupUsers
                    .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == userId && 
                                              gu.IsApproved && !gu.IsDeleted);

                if (userMembership == null)
                {
                    return ApiResponse<PagedResult<GroupMemberDto>>.Failure("Access denied", 403);
                }

                var skip = (page - 1) * pageSize;

                var membersQuery = _context.GroupUsers
                    .Include(gu => gu.User)
                    .Where(gu => gu.GroupId == groupId && gu.IsApproved && !gu.IsDeleted)
                    .OrderBy(gu => gu.JoinedAt);

                var totalCount = await membersQuery.CountAsync();
                var members = await membersQuery
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var memberDtos = _mapper.Map<List<GroupMemberDto>>(members);

                var pagedResult = new PagedResult<GroupMemberDto>
                {
                    Items = memberDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return ApiResponse<PagedResult<GroupMemberDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members for group {GroupId}", groupId);
                return ApiResponse<PagedResult<GroupMemberDto>>.Failure("Failed to get group members");
            }
        }

        /// <summary>
        /// Remove a member from group (admin/moderator only)
        /// </summary>
        public async Task<ApiResponse<bool>> RemoveMemberAsync(Guid groupId, Guid memberUserId, Guid requestingUserId)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.GroupUsers)
                    .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

                if (group == null)
                {
                    return ApiResponse<bool>.Failure("Group not found", 404);
                }

                // Check requesting user's role
                var requestingUserRole = group.GroupUsers
                    .FirstOrDefault(gu => gu.UserId == requestingUserId && !gu.IsDeleted)?.Role;

                if (requestingUserRole != "Admin" && requestingUserRole != "Moderator")
                {
                    return ApiResponse<bool>.Failure("Insufficient permissions", 403);
                }

                // Find member to remove
                var memberToRemove = group.GroupUsers
                    .FirstOrDefault(gu => gu.UserId == memberUserId && !gu.IsDeleted);

                if (memberToRemove == null)
                {
                    return ApiResponse<bool>.Failure("Member not found in group", 404);
                }

                // Cannot remove group creator
                if (group.CreatedBy == memberUserId)
                {
                    return ApiResponse<bool>.Failure("Cannot remove group creator", 400);
                }

                // Moderators cannot remove admins
                if (requestingUserRole == "Moderator" && memberToRemove.Role == "Admin")
                {
                    return ApiResponse<bool>.Failure("Moderators cannot remove admins", 403);
                }

                memberToRemove.IsDeleted = true;
                memberToRemove.LeftAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User {MemberUserId} removed from group {GroupId} by {RequestingUserId}", 
                    memberUserId, groupId, requestingUserId);
                
                return ApiResponse<bool>.Success(true, "Member removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member {MemberUserId} from group {GroupId}", memberUserId, groupId);
                return ApiResponse<bool>.Failure("Failed to remove member");
            }
        }

        /// <summary>
        /// Update member role (admin only)
        /// </summary>
        public async Task<ApiResponse<bool>> UpdateMemberRoleAsync(Guid groupId, Guid memberUserId, string role, Guid requestingUserId)
        {
            try
            {
                var validRoles = new[] { "Admin", "Moderator", "Member" };
                if (!validRoles.Contains(role))
                {
                    return ApiResponse<bool>.Failure("Invalid role", 400);
                }

                var group = await _context.Groups
                    .Include(g => g.GroupUsers)
                    .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

                if (group == null)
                {
                    return ApiResponse<bool>.Failure("Group not found", 404);
                }

                // Check requesting user's role (only admins can update roles)
                var requestingUserRole = group.GroupUsers
                    .FirstOrDefault(gu => gu.UserId == requestingUserId && !gu.IsDeleted)?.Role;

                if (requestingUserRole != "Admin")
                {
                    return ApiResponse<bool>.Failure("Only admins can update member roles", 403);
                }

                // Find member to update
                var memberToUpdate = group.GroupUsers
                    .FirstOrDefault(gu => gu.UserId == memberUserId && !gu.IsDeleted);

                if (memberToUpdate == null)
                {
                    return ApiResponse<bool>.Failure("Member not found in group", 404);
                }

                // Cannot change role of group creator
                if (group.CreatedBy == memberUserId)
                {
                    return ApiResponse<bool>.Failure("Cannot change role of group creator", 400);
                }

                memberToUpdate.Role = role;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User {MemberUserId} role updated to {Role} in group {GroupId} by {RequestingUserId}", 
                    memberUserId, role, groupId, requestingUserId);
                
                return ApiResponse<bool>.Success(true, "Member role updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for member {MemberUserId} in group {GroupId}", memberUserId, groupId);
                return ApiResponse<bool>.Failure("Failed to update member role");
            }
        }

        /// <summary>
        /// Generate invite code for private group
        /// </summary>
        public async Task<ApiResponse<string>> GenerateInviteCodeAsync(Guid groupId, Guid userId)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.GroupUsers)
                    .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

                if (group == null)
                {
                    return ApiResponse<string>.Failure("Group not found", 404);
                }

                // Check if user is admin or moderator
                var userRole = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId)?.Role;
                if (userRole != "Admin" && userRole != "Moderator")
                {
                    return ApiResponse<string>.Failure("Insufficient permissions", 403);
                }

                if (!group.IsPrivate)
                {
                    return ApiResponse<string>.Failure("Invite codes are only for private groups", 400);
                }

                group.InviteCode = GenerateInviteCode();
                group.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("New invite code generated for group {GroupId} by user {UserId}", groupId, userId);
                
                return ApiResponse<string>.Success(group.InviteCode, "Invite code generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invite code for group {GroupId}", groupId);
                return ApiResponse<string>.Failure("Failed to generate invite code");
            }
        }

        /// <summary>
        /// Generate a random invite code
        /// </summary>
        private static string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
