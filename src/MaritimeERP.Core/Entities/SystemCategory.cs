using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class SystemCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<ShipSystem> Systems { get; set; } = new List<ShipSystem>();
    }
} 