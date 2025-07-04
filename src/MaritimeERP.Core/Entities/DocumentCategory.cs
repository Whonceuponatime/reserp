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

        // Navigation properties
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
} 