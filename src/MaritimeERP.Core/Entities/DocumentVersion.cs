using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class DocumentVersion
    {
        public int Id { get; set; }

        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public short VersionNo { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public int UploadedById { get; set; }
        public User UploadedBy { get; set; } = null!;

        public DateTime UploadedAt { get; set; }

        [MaxLength(1000)]
        public string? ChangeNote { get; set; }

        public int StatusId { get; set; }
        public DocumentStatus Status { get; set; } = null!;

        // Display properties
        public string DisplayName => $"v{VersionNo} - {Document?.Filename}";
        public string VersionDisplay => $"Version {VersionNo}";
        public string UploadInfo => $"Uploaded by {UploadedBy?.FullName} on {UploadedAt:yyyy-MM-dd HH:mm}";
    }
} 