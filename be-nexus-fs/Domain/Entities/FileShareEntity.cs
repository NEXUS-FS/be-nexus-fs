using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    //we have 3 roles for file sharing which are strings.
    public static class SharePermission
    {
        public const string Viewer = "viewer";
        public const string Editor = "editor";
        public const string Owner = "owner";
    }

    public class FileShareEntity
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ResourcePath { get; set; } = String.Empty; // the specific file or folder

        [Required]
        public string UserId { get; set; } = String.Empty; // the user receiving access

        public string Permission { get; set; } = SharePermission.Viewer; // default to viewer

        public string SharedByUserId { get; set; } = String.Empty; // audit: Who granted this?
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    }
}