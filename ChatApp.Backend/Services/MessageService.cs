using ChatApp.Backend.DTOs;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Backend.Hubs;

namespace ChatApp.Backend.Services
{
    /// <summary>
    /// Service for message management operations
    /// </summary>
    public class MessageService : IMessageService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<MessageService> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessageService(
            AppDbContext context, 
            IMapper mapper, 
            ILogger<MessageService> logger,
            IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Send a new message
        /// </summary>
        public async Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageDto sendMessageDto, Guid userId)
        {
            try
            {
                // Validate user exists and is a member of the group
                var groupUser = await _context.GroupUsers
                    .Include(gu => gu.Group)
                    .FirstOrDefaultAsync(gu => gu.GroupId == sendMessageDto.GroupId && 
                                              gu.UserId == userId && 
                                              gu.IsApproved && !gu.IsDeleted);

                if (groupUser == null)
                {
                    return ApiResponse<MessageDto>.Failure("You are not a member of this group", 403);
                }

                // Validate reply message if specified
                Message? replyToMessage = null;
                if (sendMessageDto.ReplyToMessageId.HasValue)
                {
                    replyToMessage = await _context.Messages
                        .FirstOrDefaultAsync(m => m.Id == sendMessageDto.ReplyToMessageId && 
                                                  m.GroupId == sendMessageDto.GroupId && 
                                                  !m.IsDeleted);

                    if (replyToMessage == null)
                    {
                        return ApiResponse<MessageDto>.Failure("Reply message not found", 404);
                    }
                }

                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    Content = sendMessageDto.Content,
                    UserId = userId,
                    GroupId = sendMessageDto.GroupId,
                    ReplyToMessageId = sendMessageDto.ReplyToMessageId,
                    AttachmentUrls = sendMessageDto.AttachmentUrls ?? new List<string>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Load message with related data for response
                var savedMessage = await _context.Messages
                    .Include(m => m.User)
                    .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm!.User)
                    .FirstOrDefaultAsync(m => m.Id == message.Id);

                var messageDto = _mapper.Map<MessageDto>(savedMessage);

                // Send real-time notification via SignalR
                await _hubContext.Clients.Group($"group_{sendMessageDto.GroupId}")
                    .SendAsync("ReceiveMessage", messageDto);

                _logger.LogInformation("Message {MessageId} sent by user {UserId} to group {GroupId}", 
                    message.Id, userId, sendMessageDto.GroupId);

                return ApiResponse<MessageDto>.Success(messageDto, "Message sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message for user {UserId} to group {GroupId}", 
                    userId, sendMessageDto.GroupId);
                return ApiResponse<MessageDto>.Failure("Failed to send message");
            }
        }

        /// <summary>
        /// Update/edit a message
        /// </summary>
        public async Task<ApiResponse<MessageDto>> UpdateMessageAsync(Guid messageId, UpdateMessageDto updateMessageDto, Guid userId)
        {
            try
            {
                var message = await _context.Messages
                    .Include(m => m.User)
                    .Include(m => m.Group)
                    .ThenInclude(g => g.GroupUsers)
                    .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

                if (message == null)
                {
                    return ApiResponse<MessageDto>.Failure("Message not found", 404);
                }

                // Check if user can edit this message (only message author or group admin)
                var isAuthor = message.UserId == userId;
                var isGroupAdmin = message.Group.GroupUsers
                    .Any(gu => gu.UserId == userId && gu.Role == "Admin" && gu.IsApproved && !gu.IsDeleted);

                if (!isAuthor && !isGroupAdmin)
                {
                    return ApiResponse<MessageDto>.Failure("You can only edit your own messages", 403);
                }

                // Update message content
                message.Content = updateMessageDto.Content;
                message.IsEdited = true;
                message.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Load updated message with related data
                var updatedMessage = await _context.Messages
                    .Include(m => m.User)
                    .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm!.User)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                var messageDto = _mapper.Map<MessageDto>(updatedMessage);

                // Send real-time notification via SignalR
                await _hubContext.Clients.Group($"group_{message.GroupId}")
                    .SendAsync("MessageEdited", messageDto);

                _logger.LogInformation("Message {MessageId} edited by user {UserId}", messageId, userId);

                return ApiResponse<MessageDto>.Success(messageDto, "Message updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating message {MessageId} by user {UserId}", messageId, userId);
                return ApiResponse<MessageDto>.Failure("Failed to update message");
            }
        }

        /// <summary>
        /// Delete a message (soft delete)
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteMessageAsync(Guid messageId, Guid userId)
        {
            try
            {
                var message = await _context.Messages
                    .Include(m => m.Group)
                    .ThenInclude(g => g.GroupUsers)
                    .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

                if (message == null)
                {
                    return ApiResponse<bool>.Failure("Message not found", 404);
                }

                // Check if user can delete this message (message author, group admin, or moderator)
                var isAuthor = message.UserId == userId;
                var userRole = message.Group.GroupUsers
                    .FirstOrDefault(gu => gu.UserId == userId && gu.IsApproved && !gu.IsDeleted)?.Role;
                var canDelete = isAuthor || userRole == "Admin" || userRole == "Moderator";

                if (!canDelete)
                {
                    return ApiResponse<bool>.Failure("You can only delete your own messages", 403);
                }

                message.IsDeleted = true;
                message.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send real-time notification via SignalR
                await _hubContext.Clients.Group($"group_{message.GroupId}")
                    .SendAsync("MessageDeleted", messageId);

                _logger.LogInformation("Message {MessageId} deleted by user {UserId}", messageId, userId);

                return ApiResponse<bool>.Success(true, "Message deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId} by user {UserId}", messageId, userId);
                return ApiResponse<bool>.Failure("Failed to delete message");
            }
        }

        /// <summary>
        /// Get messages for a group with pagination
        /// </summary>
        public async Task<ApiResponse<MessagePageDto>> GetGroupMessagesAsync(Guid groupId, Guid userId, int page = 1, int pageSize = 50)
        {
            try
            {
                // Check if user is a member of the group
                var isMember = await _context.GroupUsers
                    .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == userId && 
                                   gu.IsApproved && !gu.IsDeleted);

                if (!isMember)
                {
                    return ApiResponse<MessagePageDto>.Failure("You are not a member of this group", 403);
                }

                var skip = (page - 1) * pageSize;

                var messagesQuery = _context.Messages
                    .Include(m => m.User)
                    .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm!.User)
                    .Where(m => m.GroupId == groupId && !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt);

                var totalCount = await messagesQuery.CountAsync();
                var messages = await messagesQuery
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var messageDtos = _mapper.Map<List<MessageDto>>(messages);

                var messagePageDto = new MessagePageDto
                {
                    Messages = messageDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasNextPage = page < Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = page > 1
                };

                return ApiResponse<MessagePageDto>.Success(messagePageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for group {GroupId}", groupId);
                return ApiResponse<MessagePageDto>.Failure("Failed to get messages");
            }
        }

        /// <summary>
        /// Search messages in a group
        /// </summary>
        public async Task<ApiResponse<MessagePageDto>> SearchMessagesAsync(MessageSearchDto searchDto, Guid userId)
        {
            try
            {
                // Check if user is a member of the group
                var isMember = await _context.GroupUsers
                    .AnyAsync(gu => gu.GroupId == searchDto.GroupId && gu.UserId == userId && 
                                   gu.IsApproved && !gu.IsDeleted);

                if (!isMember)
                {
                    return ApiResponse<MessagePageDto>.Failure("You are not a member of this group", 403);
                }

                var skip = (searchDto.Page - 1) * searchDto.PageSize;

                var messagesQuery = _context.Messages
                    .Include(m => m.User)
                    .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm!.User)
                    .Where(m => m.GroupId == searchDto.GroupId && !m.IsDeleted);

                // Apply search filters
                if (!string.IsNullOrEmpty(searchDto.SearchTerm))
                {
                    messagesQuery = messagesQuery.Where(m => m.Content.Contains(searchDto.SearchTerm));
                }

                if (searchDto.FromUserId.HasValue)
                {
                    messagesQuery = messagesQuery.Where(m => m.UserId == searchDto.FromUserId);
                }

                if (searchDto.FromDate.HasValue)
                {
                    messagesQuery = messagesQuery.Where(m => m.CreatedAt >= searchDto.FromDate);
                }

                if (searchDto.ToDate.HasValue)
                {
                    messagesQuery = messagesQuery.Where(m => m.CreatedAt <= searchDto.ToDate);
                }

                messagesQuery = messagesQuery.OrderByDescending(m => m.CreatedAt);

                var totalCount = await messagesQuery.CountAsync();
                var messages = await messagesQuery
                    .Skip(skip)
                    .Take(searchDto.PageSize)
                    .ToListAsync();

                var messageDtos = _mapper.Map<List<MessageDto>>(messages);

                var messagePageDto = new MessagePageDto
                {
                    Messages = messageDtos,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize),
                    HasNextPage = searchDto.Page < Math.Ceiling((double)totalCount / searchDto.PageSize),
                    HasPreviousPage = searchDto.Page > 1
                };

                return ApiResponse<MessagePageDto>.Success(messagePageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages in group {GroupId}", searchDto.GroupId);
                return ApiResponse<MessagePageDto>.Failure("Failed to search messages");
            }
        }

        /// <summary>
        /// Get message by ID
        /// </summary>
        public async Task<ApiResponse<MessageDto>> GetMessageAsync(Guid messageId, Guid userId)
        {
            try
            {
                var message = await _context.Messages
                    .Include(m => m.User)
                    .Include(m => m.Group)
                    .ThenInclude(g => g.GroupUsers)
                    .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm!.User)
                    .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

                if (message == null)
                {
                    return ApiResponse<MessageDto>.Failure("Message not found", 404);
                }

                // Check if user is a member of the group
                var isMember = message.Group.GroupUsers
                    .Any(gu => gu.UserId == userId && gu.IsApproved && !gu.IsDeleted);

                if (!isMember)
                {
                    return ApiResponse<MessageDto>.Failure("Access denied", 403);
                }

                var messageDto = _mapper.Map<MessageDto>(message);

                return ApiResponse<MessageDto>.Success(messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message {MessageId} for user {UserId}", messageId, userId);
                return ApiResponse<MessageDto>.Failure("Failed to get message");
            }
        }

        /// <summary>
        /// Get replies to a message
        /// </summary>
        public async Task<ApiResponse<PagedResult<MessageDto>>> GetMessageRepliesAsync(Guid messageId, Guid userId, int page = 1, int pageSize = 20)
        {
            try
            {
                // First get the original message to check group membership
                var originalMessage = await _context.Messages
                    .Include(m => m.Group)
                    .ThenInclude(g => g.GroupUsers)
                    .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

                if (originalMessage == null)
                {
                    return ApiResponse<PagedResult<MessageDto>>.Failure("Message not found", 404);
                }

                // Check if user is a member of the group
                var isMember = originalMessage.Group.GroupUsers
                    .Any(gu => gu.UserId == userId && gu.IsApproved && !gu.IsDeleted);

                if (!isMember)
                {
                    return ApiResponse<PagedResult<MessageDto>>.Failure("Access denied", 403);
                }

                var skip = (page - 1) * pageSize;

                var repliesQuery = _context.Messages
                    .Include(m => m.User)
                    .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm!.User)
                    .Where(m => m.ReplyToMessageId == messageId && !m.IsDeleted)
                    .OrderBy(m => m.CreatedAt);

                var totalCount = await repliesQuery.CountAsync();
                var replies = await repliesQuery
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var replyDtos = _mapper.Map<List<MessageDto>>(replies);

                var pagedResult = new PagedResult<MessageDto>
                {
                    Items = replyDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return ApiResponse<PagedResult<MessageDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting replies for message {MessageId}", messageId);
                return ApiResponse<PagedResult<MessageDto>>.Failure("Failed to get message replies");
            }
        }
    }
}
