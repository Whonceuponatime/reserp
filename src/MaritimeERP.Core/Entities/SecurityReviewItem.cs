using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class SecurityReviewItem
    {
        public int Id { get; set; }

        public int ChangeId { get; set; }
        public ChangeRequest ChangeRequest { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string CheckItem { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Result { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Note { get; set; }

        // Display properties
        public string DisplayName => $"{Category}: {CheckItem}";
        public string ResultDisplay => Result;
    }
} 