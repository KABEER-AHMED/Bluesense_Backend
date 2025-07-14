using ChatApp.Backend.DTOs;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace ChatApp.Backend.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly IConfiguration _configuration;

        public AuthServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            // Setup configuration
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"JwtSettings:SecretKey", "test-secret-key-for-unit-tests-32-characters-minimum"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"JwtSettings:AccessTokenExpiryMinutes", "15"},
                {"JwtSettings:RefreshTokenExpiryDays", "7"}
            });
            _configuration = configurationBuilder.Build();

            _loggerMock = new Mock<ILogger<AuthService>>();
            _authService = new AuthService(_context, _configuration, _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.User.Username.Should().Be("testuser");
            result.Data.User.Email.Should().Be("test@example.com");
            result.Data.AccessToken.Should().NotBeNullOrEmpty();
            result.Data.RefreshToken.Should().NotBeNullOrEmpty();

            // Verify user was created in database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            user.Should().NotBeNull();
            user!.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ShouldReturnError()
        {
            // Arrange
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "existinguser",
                Email = "existing@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
            };
            
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Username = "newuser",
                Email = "existing@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("already exists");
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnTokens()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AccessToken.Should().NotBeNullOrEmpty();
            result.Data.RefreshToken.Should().NotBeNullOrEmpty();
            result.Data.User.Email.Should().Be("test@example.com");
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ShouldReturnError()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "wrongpassword"
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Invalid email or password");
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var refreshToken = await _authService.GenerateRefreshTokenAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken.Token);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AccessToken.Should().NotBeNullOrEmpty();
            result.Data.RefreshToken.Should().NotBeNullOrEmpty();
            result.Data.RefreshToken.Should().NotBe(refreshToken.Token); // Should be a new token
        }

        [Fact]
        public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnError()
        {
            // Arrange
            var invalidToken = "invalid-refresh-token";

            // Act
            var result = await _authService.RefreshTokenAsync(invalidToken);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Invalid or expired");
        }

        [Fact]
        public async Task RevokeTokenAsync_WithValidToken_ShouldRevokeToken()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var refreshToken = await _authService.GenerateRefreshTokenAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.RevokeTokenAsync(refreshToken.Token);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();

            // Verify token is revoked
            var revokedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);
            revokedToken.Should().NotBeNull();
            revokedToken!.IsRevoked.Should().BeTrue();
        }

        [Fact]
        public void GenerateAccessToken_WithValidUser_ShouldReturnValidJwt()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com"
            };

            // Act
            var token = _authService.GenerateAccessToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Split('.').Should().HaveCount(3); // JWT should have 3 parts
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
