using ChatApp.Backend.DTOs;
using ChatApp.Backend.Services.Interfaces;

namespace ChatApp.Backend.Services
{
    /// <summary>
    /// Service for file upload and management
    /// </summary>
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _uploadPath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions;

        public FileService(ILogger<FileService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            _uploadPath = _configuration.GetValue<string>("FileUpload:UploadPath") ?? "uploads";
            _maxFileSize = _configuration.GetValue<long>("FileUpload:MaxFileSize", 100 * 1024 * 1024); // 100MB default
            _allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? 
                new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt" };

            // Ensure upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        /// <summary>
        /// Upload a file
        /// </summary>
        public async Task<ApiResponse<FileUploadDto>> UploadFileAsync(IFormFile file, Guid userId)
        {
            try
            {
                // Validate file
                var validationResult = ValidateFile(file);
                if (!validationResult.IsSuccess)
                {
                    return validationResult;
                }

                // Generate unique filename
                var fileName = GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(_uploadPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUploadDto = new FileUploadDto
                {
                    FileName = fileName,
                    FileUrl = $"/api/files/{fileName}",
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    UploadedAt = DateTime.UtcNow
                };

                _logger.LogInformation("File {FileName} uploaded by user {UserId}", fileName, userId);

                return ApiResponse<FileUploadDto>.Success(fileUploadDto, "File uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file for user {UserId}", userId);
                return ApiResponse<FileUploadDto>.Failure("Failed to upload file");
            }
        }

        /// <summary>
        /// Upload multiple files
        /// </summary>
        public async Task<ApiResponse<List<FileUploadDto>>> UploadFilesAsync(IFormFileCollection files, Guid userId)
        {
            try
            {
                var uploadedFiles = new List<FileUploadDto>();
                var errors = new List<string>();

                foreach (var file in files)
                {
                    var result = await UploadFileAsync(file, userId);
                    if (result.IsSuccess && result.Data != null)
                    {
                        uploadedFiles.Add(result.Data);
                    }
                    else
                    {
                        errors.AddRange(result.Errors);
                    }
                }

                if (errors.Any() && !uploadedFiles.Any())
                {
                    return ApiResponse<List<FileUploadDto>>.Failure(errors, "Failed to upload any files");
                }

                var message = errors.Any() 
                    ? $"Uploaded {uploadedFiles.Count} files successfully, {errors.Count} failed"
                    : $"All {uploadedFiles.Count} files uploaded successfully";

                return ApiResponse<List<FileUploadDto>>.Success(uploadedFiles, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple files for user {UserId}", userId);
                return ApiResponse<List<FileUploadDto>>.Failure("Failed to upload files");
            }
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteFileAsync(string fileName, Guid userId)
        {
            try
            {
                var filePath = Path.Combine(_uploadPath, fileName);

                if (!File.Exists(filePath))
                {
                    return ApiResponse<bool>.Failure("File not found", 404);
                }

                // Validate filename to prevent path traversal
                if (!IsValidFileName(fileName))
                {
                    return ApiResponse<bool>.Failure("Invalid file name", 400);
                }

                File.Delete(filePath);

                _logger.LogInformation("File {FileName} deleted by user {UserId}", fileName, userId);

                return ApiResponse<bool>.Success(true, "File deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName} for user {UserId}", fileName, userId);
                return ApiResponse<bool>.Failure("Failed to delete file");
            }
        }

        /// <summary>
        /// Get file information
        /// </summary>
        public async Task<ApiResponse<FileUploadDto>> GetFileInfoAsync(string fileName)
        {
            try
            {
                // Validate filename to prevent path traversal
                if (!IsValidFileName(fileName))
                {
                    return ApiResponse<FileUploadDto>.Failure("Invalid file name", 400);
                }

                var filePath = Path.Combine(_uploadPath, fileName);

                if (!File.Exists(filePath))
                {
                    return ApiResponse<FileUploadDto>.Failure("File not found", 404);
                }

                var fileInfo = new FileInfo(filePath);
                var contentType = GetContentType(fileName);

                var fileUploadDto = new FileUploadDto
                {
                    FileName = fileName,
                    FileUrl = $"/api/files/{fileName}",
                    FileSize = fileInfo.Length,
                    ContentType = contentType,
                    UploadedAt = fileInfo.CreationTimeUtc
                };

                return ApiResponse<FileUploadDto>.Success(fileUploadDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info for {FileName}", fileName);
                return ApiResponse<FileUploadDto>.Failure("Failed to get file information");
            }
        }

        /// <summary>
        /// Download a file
        /// </summary>
        public async Task<ApiResponse<(byte[] FileBytes, string ContentType, string FileName)>> DownloadFileAsync(string fileName)
        {
            try
            {
                // Validate filename to prevent path traversal
                if (!IsValidFileName(fileName))
                {
                    return ApiResponse<(byte[], string, string)>.Failure("Invalid file name", 400);
                }

                var filePath = Path.Combine(_uploadPath, fileName);

                if (!File.Exists(filePath))
                {
                    return ApiResponse<(byte[], string, string)>.Failure("File not found", 404);
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(fileName);

                return ApiResponse<(byte[], string, string)>.Success((fileBytes, contentType, fileName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileName}", fileName);
                return ApiResponse<(byte[], string, string)>.Failure("Failed to download file");
            }
        }

        /// <summary>
        /// Get file stream for efficient streaming
        /// </summary>
        public async Task<ApiResponse<(Stream FileStream, string ContentType, string FileName)>> GetFileStreamAsync(string fileName)
        {
            try
            {
                // Validate filename to prevent path traversal
                if (!IsValidFileName(fileName))
                {
                    return ApiResponse<(Stream, string, string)>.Failure("Invalid file name", 400);
                }

                var filePath = Path.Combine(_uploadPath, fileName);

                if (!File.Exists(filePath))
                {
                    return ApiResponse<(Stream, string, string)>.Failure("File not found", 404);
                }

                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var contentType = GetContentType(fileName);

                return ApiResponse<(Stream, string, string)>.Success((fileStream, contentType, fileName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file stream for {FileName}", fileName);
                return ApiResponse<(Stream, string, string)>.Failure("Failed to get file stream");
            }
        }

        /// <summary>
        /// Validate uploaded file
        /// </summary>
        private ApiResponse<FileUploadDto> ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return ApiResponse<FileUploadDto>.Failure("No file provided", 400);
            }

            if (file.Length > _maxFileSize)
            {
                return ApiResponse<FileUploadDto>.Failure($"File size exceeds maximum limit of {_maxFileSize / (1024 * 1024)}MB", 400);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return ApiResponse<FileUploadDto>.Failure($"File type {extension} is not allowed", 400);
            }

            // Additional security check for content type
            if (string.IsNullOrEmpty(file.ContentType))
            {
                return ApiResponse<FileUploadDto>.Failure("Invalid file content type", 400);
            }

            return ApiResponse<FileUploadDto>.Success(null!); // This is just for validation
        }

        /// <summary>
        /// Generate unique filename to prevent conflicts
        /// </summary>
        private static string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var guid = Guid.NewGuid().ToString("N")[..8]; // First 8 characters

            return $"{fileNameWithoutExtension}_{timestamp}_{guid}{extension}";
        }

        /// <summary>
        /// Validate filename to prevent path traversal attacks
        /// </summary>
        private static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            // Check for path traversal attempts
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                return false;

            // Check for invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.Any(c => invalidChars.Contains(c)))
                return false;

            return true;
        }

        /// <summary>
        /// Get content type based on file extension
        /// </summary>
        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".xml" => "text/xml",
                ".json" => "application/json",
                _ => "application/octet-stream"
            };
        }
    }
}
