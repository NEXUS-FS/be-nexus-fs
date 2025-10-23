namespace Application.DTOs
{
    public class FileOperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Content { get; set; } 
        public DateTime Timestamp { get; set; }
    }
}
