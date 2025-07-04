using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
    }
} 