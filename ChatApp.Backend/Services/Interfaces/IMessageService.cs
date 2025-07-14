using ChatApp.Backend.DTOs;

namespace ChatApp.Backend.Services.Interfaces
{
    /// <summary>
    /// Interface for message management operations
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Send a new message
        /// </summary>
        Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageDto sendMessageDto, Guid userId);

        /// <summary>
        /// Update/edit a message
        /// </summary>
        Task<ApiResponse<MessageDto>> UpdateMessageAsync(Guid messageId, UpdateMessageDto updateMessageDto, Guid userId);

        /// <summary>
        /// Delete a message (soft delete)
        /// </summary>
        Task<ApiResponse<bool>> DeleteMessageAsync(Guid messageId, Guid userId);

        /// <summary>
        /// Get messages for a group with pagination
        /// </summary>
        Task<ApiResponse<MessagePageDto>> GetGroupMessagesAsync(Guid groupId, Guid userId, int page = 1, int pageSize = 50);

        /// <summary>
        /// Search messages in a group
        /// </summary>
        Task<ApiResponse<MessagePageDto>> SearchMessagesAsync(MessageSearchDto searchDto, Guid userId);

        /// <summary>
        /// Get message by ID
        /// </summary>
        Task<ApiResponse<MessageDto>> GetMessageAsync(Guid messageId, Guid userId);

        /// <summary>
        /// Get replies to a message
        /// </summary>
        Task<ApiResponse<PagedResult<MessageDto>>> GetMessageRepliesAsync(Guid messageId, Guid userId, int page = 1, int pageSize = 20);
    }
}
