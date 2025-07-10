using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class Approval
    {
        public int Id { get; set; }

        public int ChangeId { get; set; }
        
        public int ChangeRequestId { get; set; }
        public ChangeRequest ChangeRequest { get; set; } = null!;

        public short Stage { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        public int ActionById { get; set; }
        public User ActionBy { get; set; } = null!;

        public DateTime ActionAt { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        // Display properties
        public string DisplayName => $"Stage {Stage}: {Action}";
        public string ActionDisplay => $"{Action} by {ActionBy?.FullName} on {ActionAt:yyyy-MM-dd HH:mm}";
    }
} 