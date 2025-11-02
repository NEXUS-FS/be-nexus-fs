using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Entity representing a user in the NexusFS system.
    /// Supports Basic Authentication (username/password), OAuth (Google), and Clerk authentication.
    /// </summary>
    public class UserEntity
    {
        /// <summary>
        /// Unique identifier for the user.
        /// </summary>
        [Key]
        [MaxLength(100)]
        public required string Id { get; set; }

        /// <summary>
        /// Username for login (used in Basic Auth).
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string Username { get; set; }

        /// <summary>
        /// Email address (required for both Basic Auth and OAuth).
        /// </summary>
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public required string Email { get; set; }

        /// <summary>
        /// Hashed password (nullable for OAuth users who don't have passwords).
        /// Only used for Basic Authentication.
        /// </summary>
        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        /// <summary>
        /// User role: "User" or "Admin".
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string Role { get; set; }

        /// <summary>
        /// Authentication provider: "Basic", "Google", or "Clerk".
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string Provider { get; set; }

        /// <summary>
        /// Provider-specific user ID (e.g., Google user ID for OAuth users, Clerk user ID for Clerk users).
        /// Nullable for Basic Auth users.
        /// </summary>
        [MaxLength(255)]
        public string? ProviderId { get; set; }

        /// <summary>
        /// Indicates if the user account is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Date and time when the user was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the user was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Date and time of the user's last login.
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Date and time when the user was deleted (for soft delete).
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Refresh token for JWT authentication (optional).
        /// </summary>
        [MaxLength(500)]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Expiration date for the refresh token.
        /// </summary>
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
