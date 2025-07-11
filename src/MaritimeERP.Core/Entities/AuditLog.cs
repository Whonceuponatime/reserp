using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string EntityId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? EntityName { get; set; }

        [MaxLength(50)]
        public string? TableName { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? UserId { get; set; }
        public User? User { get; set; }

        [MaxLength(200)]
        public string? UserName { get; set; }

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(500)]
        public string? AdditionalInfo { get; set; }

        // Display properties
        public string ActionDisplay => Action switch
        {
            "CREATE" => "Created",
            "UPDATE" => "Updated",
            "DELETE" => "Deleted",
            "APPROVE" => "Approved",
            "REJECT" => "Rejected",
            "SUBMIT" => "Submitted",
            "CANCEL" => "Cancelled",
            "ACTIVATE" => "Activated",
            "DEACTIVATE" => "Deactivated",
            _ => Action
        };

        public string TimestampDisplay => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        
        public string UserDisplay => !string.IsNullOrEmpty(UserName) ? UserName : $"User {UserId}";
        
        public string EntityDisplay => !string.IsNullOrEmpty(EntityName) ? EntityName : $"{EntityType} {EntityId}";
    }
} 