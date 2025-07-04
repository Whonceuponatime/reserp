using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class Document
    {
        public int Id { get; set; }

        public int ShipId { get; set; }
        public Ship Ship { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string Filename { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string StoragePath { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public DocumentCategory Category { get; set; } = null!;

        public int StatusId { get; set; }
        public DocumentStatus Status { get; set; } = null!;

        [MaxLength(1000)]
        public string? Comments { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

        // Display properties
        public string DisplayName => Filename;
        public string StatusDisplay => Status?.Name ?? "Unknown";
        public string CategoryDisplay => Category?.Name ?? "Unknown";
        public DocumentVersion? LatestVersion => Versions.OrderByDescending(v => v.VersionNo).FirstOrDefault();
    }
} 