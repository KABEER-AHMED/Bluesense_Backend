using ChatApp.Backend.DTOs;
using ChatApp.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Backend.Controllers
{
    /// <summary>
    /// Controller for message operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessageController> _logger;

        public MessageController(IMessageService messageService, ILogger<MessageController> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        /// <summary>
        /// Send a new message
        /// </summary>
        /// <param name="sendMessageDto">Message details</param>
        /// <returns>Sent message information</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage([FromBody] SendMessageDto sendMessageDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.SendMessageAsync(sendMessageDto, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("not a member"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, ApiResponse<MessageDto>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Update/edit a message
        /// </summary>
        /// <param name="id">Message ID</param>
        /// <param name="updateMessageDto">Message update details</param>
        /// <returns>Updated message information</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<MessageDto>>> UpdateMessage(Guid id, [FromBody] UpdateMessageDto updateMessageDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.UpdateMessageAsync(id, updateMessageDto, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("only edit your own"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating message {MessageId}", id);
                return StatusCode(500, ApiResponse<MessageDto>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Delete a message
        /// </summary>
        /// <param name="id">Message ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteMessage(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.DeleteMessageAsync(id, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("only delete your own"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", id);
                return StatusCode(500, ApiResponse<bool>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Get message by ID
        /// </summary>
        /// <param name="id">Message ID</param>
        /// <returns>Message information</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<MessageDto>>> GetMessage(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.GetMessageAsync(id, userId);

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
                _logger.LogError(ex, "Error getting message {MessageId}", id);
                return StatusCode(500, ApiResponse<MessageDto>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Get replies to a message
        /// </summary>
        /// <param name="id">Message ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <returns>Paginated list of replies</returns>
        [HttpGet("{id}/replies")]
        public async Task<ActionResult<ApiResponse<PagedResult<MessageDto>>>> GetMessageReplies(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.GetMessageRepliesAsync(id, userId, page, pageSize);

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
                _logger.LogError(ex, "Error getting replies for message {MessageId}", id);
                return StatusCode(500, ApiResponse<PagedResult<MessageDto>>.Failure("An unexpected error occurred"));
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

    /// <summary>
    /// Controller for group-specific message operations
    /// </summary>
    [ApiController]
    [Route("api/groups/{groupId}/messages")]
    [Authorize]
    public class GroupMessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<GroupMessageController> _logger;

        public GroupMessageController(IMessageService messageService, ILogger<GroupMessageController> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        /// <summary>
        /// Get messages for a group
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <returns>Paginated list of group messages</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<MessagePageDto>>> GetGroupMessages(
            Guid groupId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.GetGroupMessagesAsync(groupId, userId, page, pageSize);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not a member"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for group {GroupId}", groupId);
                return StatusCode(500, ApiResponse<MessagePageDto>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Search messages in a group
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="searchTerm">Search term</param>
        /// <param name="fromUserId">Filter by user ID (optional)</param>
        /// <param name="fromDate">Filter from date (optional)</param>
        /// <param name="toDate">Filter to date (optional)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <returns>Paginated list of matching messages</returns>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<MessagePageDto>>> SearchMessages(
            Guid groupId,
            [FromQuery] string? searchTerm,
            [FromQuery] Guid? fromUserId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetUserId();
                var searchDto = new MessageSearchDto
                {
                    GroupId = groupId,
                    SearchTerm = searchTerm,
                    FromUserId = fromUserId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _messageService.SearchMessagesAsync(searchDto, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not a member"))
                        return Forbid();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages in group {GroupId}", groupId);
                return StatusCode(500, ApiResponse<MessagePageDto>.Failure("An unexpected error occurred"));
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
