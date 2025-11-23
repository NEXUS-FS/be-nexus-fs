using System.Collections.Generic;

namespace Domain.Entities
{
    public class SandboxPolicy
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;

        // Rules
        public bool IsReadOnly { get; set; } = false; // locks the whole sandbox
        public int MaxPathLength { get; set; } = 255; // prevent OS errors
        public bool AllowDotFiles { get; set; } = false; // block .env, .git, etc.

        // list of dangerous extensions (e.g. .exe, .sh, .bat)
        public List<string> BlockedFileExtensions { get; set; } = new();
    }
}