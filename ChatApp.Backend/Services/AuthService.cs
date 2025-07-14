using ChatApp.Backend.DTOs;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Securit                if (storedToken == null)
                {
                    return new ApiResponse<bool>
                    {
                        IsSuccess = false,
                        Message = "Token not found",
                        Data = false
                    };
                }
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace ChatApp.Backend.Services
{
    /// <summary>
    /// Service for handling authentication, JWT tokens, and user management
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email || u.Username == registerDto.Username);

                if (existingUser != null)
                {
                    return new ApiResponse<AuthResponseDto>
                    {
                        IsSuccess = false,
                        Message = "User with this email or username already exists",
                        Errors = new List<string> { "Email or username already in use" }
                    };
                }

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                // Create new user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate tokens
                var accessToken = GenerateAccessToken(user);
                var refreshToken = await GenerateRefreshTokenAsync(user);

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt
                    }
                };

                _logger.LogInformation("User {Username} registered successfully", user.Username);

                return new ApiResponse<AuthResponseDto>
                {
                    IsSuccess = true,
                    Message = "User registered successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration");
                return new ApiResponse<AuthResponseDto>
                {
                    IsSuccess = false,
                    Message = "An error occurred during registration",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
        {
            try
            {
                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email && !u.IsDeleted);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return new ApiResponse<AuthResponseDto>
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "Authentication failed" }
                    };
                }

                // Update last active time
                user.LastActiveAt = DateTime.UtcNow;
                user.Status = "Online";
                await _context.SaveChangesAsync();

                // Generate tokens
                var accessToken = GenerateAccessToken(user);
                var refreshToken = await GenerateRefreshTokenAsync(user);

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt
                    }
                };

                _logger.LogInformation("User {Username} logged in successfully", user.Username);

                return new ApiResponse<AuthResponseDto>
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login");
                return new ApiResponse<AuthResponseDto>
                {
                    IsSuccess = false,
                    Message = "An error occurred during login",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var storedToken = await ValidateRefreshTokenAsync(refreshToken);
                if (storedToken == null)
                {
                    return new ApiResponse<AuthResponseDto>
                    {
                        IsSuccess = false,
                        Message = "Invalid or expired refresh token",
                        Errors = new List<string> { "Token validation failed" }
                    };
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == storedToken.UserId && !u.IsDeleted);

                if (user == null)
                {
                    return new ApiResponse<AuthResponseDto>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        Errors = new List<string> { "User associated with token not found" }
                    };
                }

                // Revoke old refresh token
                storedToken.RevokedAt = DateTime.UtcNow;

                // Generate new tokens
                var accessToken = GenerateAccessToken(user);
                var newRefreshToken = await GenerateRefreshTokenAsync(user, storedToken.DeviceInfo, storedToken.IpAddress);

                await _context.SaveChangesAsync();

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt
                    }
                };

                return new ApiResponse<AuthResponseDto>
                {
                    IsSuccess = true,
                    Message = "Token refreshed successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token refresh");
                return new ApiResponse<AuthResponseDto>
                {
                    IsSuccess = false,
                    Message = "An error occurred during token refresh",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                var storedToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (storedToken == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Token not found",
                        Data = false
                    };
                }

                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Token revoked successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token revocation");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred during token revocation",
                    Data = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> RevokeAllTokensAsync(Guid userId)
        {
            try
            {
                var userTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                    .ToListAsync();

                foreach (var token in userTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "All tokens revoked successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during all tokens revocation for user {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred during tokens revocation",
                    Data = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public string GenerateAccessToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(User user, string? deviceInfo = null, string? ipAddress = null)
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = GenerateRandomToken(),
                Jti = Guid.NewGuid().ToString(),
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);
            return refreshToken;
        }

        public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token && rt.IsActive);
        }

        private string GenerateRandomToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[64];
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private int GetAccessTokenExpiryMinutes()
        {
            return _configuration.GetValue<int>("JwtSettings:AccessTokenExpiryMinutes", 15);
        }

        private int GetRefreshTokenExpiryDays()
        {
            return _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryDays", 7);
        }
    }
}
