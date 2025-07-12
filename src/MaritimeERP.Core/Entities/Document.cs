using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class Document
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string FileExtension { get; set; } = string.Empty;

        [Required]
        public long FileSizeBytes { get; set; }

        [Required]
        [MaxLength(32)]
        public string FileHash { get; set; } = string.Empty; // MD5 hash for duplicate detection

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty; // Physical file storage path

        [Required]
        [MaxLength(200)]
        public string ContentType { get; set; } = string.Empty; // MIME type

        public int CategoryId { get; set; }
        public DocumentCategory Category { get; set; } = null!;

        public int? ShipId { get; set; }
        public Ship? Ship { get; set; }

        public int UploadedByUserId { get; set; }
        public User UploadedBy { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;

        public int? ApprovedByUserId { get; set; }
        public User? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }

        // Version tracking
        public int Version { get; set; } = 1;
        public int? PreviousVersionId { get; set; }
        public Document? PreviousVersion { get; set; }

        // Navigation properties
        public ICollection<Document> NewerVersions { get; set; } = new List<Document>();
        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

        // Display properties
        public string DisplayName => $"{Name} v{Version}";
        public string StatusDisplay => IsActive ? (IsApproved ? "Approved" : "Pending Approval") : "Inactive";
        public string FileSizeDisplay => FormatFileSize(FileSizeBytes);
        public string UploadedAtDisplay => UploadedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        public string ApprovedAtDisplay => ApprovedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Not Approved";
        public string UploaderDisplay => UploadedBy?.FullName ?? "Unknown";
        public string ApproverDisplay => ApprovedBy?.FullName ?? "Not Approved";

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
            return $"{bytes / (1024 * 1024 * 1024):F1} GB";
        }
    }
} 