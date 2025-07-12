using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class ShipSystem
    {
        public int Id { get; set; }

        public int ShipId { get; set; }
        public Ship Ship { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Manufacturer { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool HasRemoteConnection { get; set; } = false;

        [MaxLength(200)]
        public string? SecurityZone { get; set; }

        public int CategoryId { get; set; }
        public SystemCategory Category { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Component> Components { get; set; } = new List<Component>();

        // Display properties
        public string DisplayName => $"{Name} ({Manufacturer} {Model})";
        public string FullDescription => $"{DisplayName} - {Description}";
    }
} 