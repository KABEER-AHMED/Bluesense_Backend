using ChatApp.Backend.DTOs;
using ChatApp.Backend.Models;

namespace ChatApp.Backend.Services.Interfaces
{
    /// <summary>
    /// Interface for authentication and JWT token management
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Register a new user
        /// </summary>
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);

        /// <summary>
        /// Authenticate user and return JWT tokens
        /// </summary>
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Revoke a refresh token
        /// </summary>
        Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken);

        /// <summary>
        /// Revoke all tokens for a user (logout from all devices)
        /// </summary>
        Task<ApiResponse<bool>> RevokeAllTokensAsync(Guid userId);

        /// <summary>
        /// Generate JWT access token
        /// </summary>
        string GenerateAccessToken(User user);

        /// <summary>
        /// Generate refresh token
        /// </summary>
        Task<RefreshToken> GenerateRefreshTokenAsync(User user, string? deviceInfo = null, string? ipAddress = null);

        /// <summary>
        /// Validate refresh token
        /// </summary>
        Task<RefreshToken?> ValidateRefreshTokenAsync(string token);
    }
}
