using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class ChangeRequest
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestNo { get; set; } = string.Empty;

        public int? ShipId { get; set; }
        public Ship? Ship { get; set; }

        public int RequestTypeId { get; set; }
        public ChangeType RequestType { get; set; } = null!;

        public int StatusId { get; set; }
        public ChangeStatus Status { get; set; } = null!;

        public int RequestedById { get; set; }
        public User RequestedBy { get; set; } = null!;

        public DateTime RequestedAt { get; set; }

        [Required]
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
        public HardwareChangeDetail? HardwareChangeDetail { get; set; }
        public SoftwareChangeDetail? SoftwareChangeDetail { get; set; }
        public SystemPlanDetail? SystemPlanDetail { get; set; }
        public ICollection<SecurityReviewItem> SecurityReviewItems { get; set; } = new List<SecurityReviewItem>();

        // Display properties
        public string DisplayName => $"{RequestNo} - {Purpose}";
        public string StatusDisplay => Status?.Name ?? "Unknown";
        public string TypeDisplay => RequestType?.Name ?? "Unknown";
        public string ShipDisplay => Ship?.ShipName ?? "N/A";
        public string CreatedAtDisplay => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd");
        public string RequestedAtDisplay => RequestedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
} 