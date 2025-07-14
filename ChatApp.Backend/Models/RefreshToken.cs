using System;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.Models
{
    /// <summary>
    /// Represents a refresh token for JWT authentication
    /// </summary>
    public class RefreshToken
    {
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// JTI (JWT ID) for tracking token families
        /// </summary>
        public string? Jti { get; set; }

        /// <summary>
        /// Device/client identifier for token tracking
        /// </summary>
        [MaxLength(100)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// IP address where token was issued
        /// </summary>
        [MaxLength(45)] // IPv6 max length
        public string? IpAddress { get; set; }

        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates if this token is currently valid and active
        /// </summary>
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

        /// <summary>
        /// Indicates if this token has expired
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// Indicates if this token has been revoked
        /// </summary>
        public bool IsRevoked => RevokedAt.HasValue;
    }
} 