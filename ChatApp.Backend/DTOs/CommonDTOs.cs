namespace ChatApp.Backend.DTOs
{
    /// <summary>
    /// DTO for file upload response
    /// </summary>
    public class FileUploadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// DTO for API response wrapper
    /// </summary>
    /// <typeparam name="T">Response data type</typeparam>
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Create a successful response
        /// </summary>
        public static ApiResponse<T> Success(T data, string message = "")
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// Create a failure response
        /// </summary>
        public static ApiResponse<T> Failure(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = new List<string> { message }
            };
        }

        /// <summary>
        /// Create a failure response with multiple errors
        /// </summary>
        public static ApiResponse<T> Failure(List<string> errors, string message = "")
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// DTO for paginated results
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
