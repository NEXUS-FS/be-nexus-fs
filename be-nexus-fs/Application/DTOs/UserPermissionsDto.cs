namespace Application.DTOs
{
    /// <summary>
    /// DTO for user permissions response.
    /// </summary>
    public class UserPermissionsDto
    {
        public required string Username { get; set; }
        public required List<string> Permissions { get; set; }
        public int TotalPermissions => Permissions?.Count ?? 0;
    }
}