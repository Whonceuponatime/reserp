using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class DocumentCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty; // e.g., "Approved Supplier Documentation"

        public bool IsRequired { get; set; } = false; // Whether this document type is mandatory

        [MaxLength(500)]
        public string? AllowedFileTypes { get; set; } = "pdf,doc,docx"; // Comma-separated list

        public long? MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB default

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0; // For sorting in UI

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Document> Documents { get; set; } = new List<Document>();

        // Display properties
        public string DisplayName => $"{Category} - {Name}";
        public string StatusDisplay => IsActive ? "Active" : "Inactive";
        public string RequiredDisplay => IsRequired ? "Required" : "Optional";
        public string MaxFileSizeDisplay => MaxFileSizeBytes.HasValue ? FormatFileSize(MaxFileSizeBytes.Value) : "No Limit";

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
            return $"{bytes / (1024 * 1024 * 1024):F1} GB";
        }
    }
} 