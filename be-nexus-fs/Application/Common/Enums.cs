namespace Application.Common
{
    /// <summary>
    /// Enumeration of file operation types supported by the system.
    /// Used across DTOs, security validation, and business logic.
    /// </summary>
    public enum FileOperation
    {
        Read,
        Write,
        Delete,
        List,
        Create,
        Move,
        Copy
    }
}