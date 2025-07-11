using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class DocumentVersion
    {
        public int Id { get; set; }

        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public int VersionNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public long FileSizeBytes { get; set; }

        [Required]
        [MaxLength(32)]
        public string FileHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ContentType { get; set; } = string.Empty;

        public int UploadedByUserId { get; set; }
        public User UploadedBy { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? ChangeDescription { get; set; }

        public bool IsActive { get; set; } = true;

        // Display properties
        public string DisplayName => $"v{VersionNumber} - {FileName}";
        public string FileSizeDisplay => FormatFileSize(FileSizeBytes);
        public string UploadedAtDisplay => UploadedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        public string UploaderDisplay => UploadedBy?.FullName ?? "Unknown";

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
            return $"{bytes / (1024 * 1024 * 1024):F1} GB";
        }
    }
} 