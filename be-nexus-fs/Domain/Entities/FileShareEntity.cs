using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public enum SharePermission
    {
        Viewer = 1, // Can only Read
        Editor = 2, // Can Read, Write, Modify content
        Owner = 3   // Can Delete, Share, Change Permissions
    }

    public class FileShareEntity
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ResourcePath { get; set; }= String.Empty; // the specific file or folder

        [Required]
        public string UserId { get; set; } = String.Empty; // the user receiving access

        public SharePermission Permission { get; set; } // Viewer, Editor, etc.

        public string SharedByUserId { get; set; }= String.Empty; // audit: Who granted this?
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    }
}