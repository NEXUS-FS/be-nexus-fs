using Application.Common;

namespace Application.DTOs
{
    public class FileOperationRequest
    {
        public required FileOperation Operation { get; set; }
        public required string ProviderId { get; set; }
        public required string FilePath { get; set; }
        public string? Content { get; set; } // For write operations - optional
        public required string UserId { get; set; }
    }
}
