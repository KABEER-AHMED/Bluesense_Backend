using ChatApp.Backend.DTOs;
using ChatApp.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Backend.Controllers
{
    /// <summary>
    /// Controller for file upload and download operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FileController> _logger;

        public FileController(IFileService fileService, ILogger<FileController> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a single file
        /// </summary>
        /// <param name="file">File to upload</param>
        /// <returns>Upload result with file information</returns>
        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<FileUploadDto>>> UploadFile(IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                var result = await _fileService.UploadFileAsync(file, userId);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, ApiResponse<FileUploadDto>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Upload multiple files
        /// </summary>
        /// <param name="files">Files to upload</param>
        /// <returns>Upload results with file information</returns>
        [HttpPost("upload-multiple")]
        public async Task<ActionResult<ApiResponse<List<FileUploadDto>>>> UploadFiles(IFormFileCollection files)
        {
            try
            {
                var userId = GetUserId();
                var result = await _fileService.UploadFilesAsync(files, userId);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple files");
                return StatusCode(500, ApiResponse<List<FileUploadDto>>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Download a file
        /// </summary>
        /// <param name="fileName">Name of the file to download</param>
        /// <returns>File content</returns>
        [HttpGet("{fileName}")]
        [AllowAnonymous] // Allow anonymous access for file downloads (can be restricted later based on requirements)
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                var result = await _fileService.GetFileStreamAsync(fileName);

                if (result == null)
                {
                    return NotFound(new { message = "File not found" });
                }

                return File(result.Value.stream, result.Value.contentType, result.Value.fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileName}", fileName);
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        /// <summary>
        /// Get file information
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <returns>File information</returns>
        [HttpGet("{fileName}/info")]
        public async Task<ActionResult<ApiResponse<FileUploadDto>>> GetFileInfo(string fileName)
        {
            try
            {
                var result = await _fileService.GetFileInfoAsync(fileName);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("Invalid"))
                        return BadRequest(result);
                    return StatusCode(500, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info for {FileName}", fileName);
                return StatusCode(500, ApiResponse<FileUploadDto>.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        /// <param name="fileName">Name of the file to delete</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{fileName}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteFile(string fileName)
        {
            try
            {
                var userId = GetUserId();
                var result = await _fileService.DeleteFileAsync(fileName, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);
                    if (result.Message.Contains("Invalid"))
                        return BadRequest(result);
                    return StatusCode(500, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName}", fileName);
                return StatusCode(500, ApiResponse<bool>.Failure("An unexpected error occurred"));
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
