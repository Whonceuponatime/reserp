using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class ChangeStatus
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
        public ICollection<ChangeRequest> ChangeRequests { get; set; } = new List<ChangeRequest>();
    }
} 