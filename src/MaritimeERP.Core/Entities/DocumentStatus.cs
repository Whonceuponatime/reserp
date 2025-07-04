using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class DocumentStatus
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; } // For UI display

        // Navigation properties
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();
    }
} 