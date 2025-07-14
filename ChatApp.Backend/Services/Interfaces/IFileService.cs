using ChatApp.Backend.DTOs;

namespace ChatApp.Backend.Services.Interfaces
{
    /// <summary>
    /// Interface for file upload and management
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Upload a file
        /// </summary>
        Task<ApiResponse<FileUploadDto>> UploadFileAsync(IFormFile file, Guid userId);

        /// <summary>
        /// Upload multiple files
        /// </summary>
        Task<ApiResponse<List<FileUploadDto>>> UploadFilesAsync(IFormFileCollection files, Guid userId);

        /// <summary>
        /// Delete a file
        /// </summary>
        Task<ApiResponse<bool>> DeleteFileAsync(string fileName, Guid userId);

        /// <summary>
        /// Get file information
        /// </summary>
        Task<ApiResponse<FileUploadDto>> GetFileInfoAsync(string fileName);

        /// <summary>
        /// Check if file exists
        /// </summary>
        Task<bool> FileExistsAsync(string fileName);

        /// <summary>
        /// Get file stream for download
        /// </summary>
        Task<(Stream stream, string contentType, string fileName)?> GetFileStreamAsync(string fileName);
    }
}
