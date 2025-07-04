using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class Software
    {
        public int Id { get; set; }

        public int ComponentId { get; set; }
        public Component Component { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Manufacturer { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        public DateTime? LastUpdated { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Display properties
        public string DisplayName => $"{Name} v{Version}";
        public string FullInfo => $"{ProductName} ({Manufacturer}) - {Purpose}";
        public string VersionInfo => $"Version: {Version}" + (LastUpdated.HasValue ? $" (Updated: {LastUpdated.Value:yyyy-MM-dd})" : "");
    }
} 