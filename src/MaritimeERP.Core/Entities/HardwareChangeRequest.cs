using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class HardwareChangeRequest
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestNumber { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        // Requester information
        public int RequesterUserId { get; set; }
        public User RequesterUser { get; set; } = null!;

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? PositionTitle { get; set; }

        [MaxLength(100)]
        public string? RequesterName { get; set; }

        // Current installation details
        [MaxLength(255)]
        public string? InstalledCbs { get; set; }

        [MaxLength(255)]
        public string? InstalledComponent { get; set; }

        // Change rationale
        public string? Reason { get; set; }

        // Hardware before the change
        [MaxLength(255)]
        public string? BeforeHwManufacturerModel { get; set; }

        [MaxLength(255)]
        public string? BeforeHwName { get; set; }

        [MaxLength(255)]
        public string? BeforeHwOs { get; set; }

        // Hardware after the change
        [MaxLength(255)]
        public string? AfterHwManufacturerModel { get; set; }

        [MaxLength(255)]
        public string? AfterHwName { get; set; }

        [MaxLength(255)]
        public string? AfterHwOs { get; set; }

        // Work description and security review
        public string? WorkDescription { get; set; }
        public string? SecurityReviewComment { get; set; }

        // Approval workflow
        public int? PreparedByUserId { get; set; }
        public User? PreparedByUser { get; set; }

        public int? ReviewedByUserId { get; set; }
        public User? ReviewedByUser { get; set; }

        public int? ApprovedByUserId { get; set; }
        public User? ApprovedByUser { get; set; }

        public DateTime? PreparedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        // Display properties
        public string DisplayName => $"{RequestNumber} - {RequesterName}";
        public string StatusDisplay => Status;
        public string CreatedDateDisplay => CreatedDate.ToString("yyyy-MM-dd");
    }
} 