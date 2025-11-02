using System.Text.Json.Serialization;

namespace Application.DTOs.Clerk
{
    /// <summary>
    /// Clerk User API response models for Application layer
    /// </summary>
    public class ClerkUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<ClerkEmailAddressDto> EmailAddresses { get; set; } = new();
        public List<ClerkPhoneNumberDto> PhoneNumbers { get; set; } = new();
        public string? ImageUrl { get; set; }
        public bool HasImage { get; set; }
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
        public long? LastSignInAt { get; set; }
        public bool Banned { get; set; }
        public bool Locked { get; set; }
        public int? LockoutExpiresInSeconds { get; set; }
        public int VerificationAttemptsRemaining { get; set; }

        // Helper properties
        public string? PrimaryEmail => EmailAddresses.FirstOrDefault(e => e.Id == PrimaryEmailAddressId)?.EmailAddress;
        public string? PrimaryEmailAddressId { get; set; }
        public string? PrimaryPhoneNumberId { get; set; }
    }

    public class ClerkEmailAddressDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public ClerkVerificationDto? Verification { get; set; }
        public List<object> LinkedTo { get; set; } = new();
    }

    public class ClerkPhoneNumberDto
    {
        public string Id { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool ReservedForSecondFactor { get; set; }
        public bool DefaultSecondFactor { get; set; }
        public ClerkVerificationDto? Verification { get; set; }
        public List<object> LinkedTo { get; set; } = new();
    }

    public class ClerkVerificationDto
    {
        public string Status { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public int? Attempts { get; set; }
        public long? ExpireAt { get; set; }
    }

    public class ClerkUsersResponseDto
    {
        public List<ClerkUserDto> Data { get; set; } = new();
        public int TotalCount { get; set; }
    }
}