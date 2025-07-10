using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class SoftwareChangeRequest
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string RequestNumber { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; }
        
        [Required]
        public int RequesterUserId { get; set; }
        public User RequesterUser { get; set; } = null!;
        
        [MaxLength(200)]
        public string? Department { get; set; }
        
        [MaxLength(200)]
        public string? PositionTitle { get; set; }
        
        [MaxLength(200)]
        public string? RequesterName { get; set; }
        
        [MaxLength(500)]
        public string? InstalledCbs { get; set; }
        
        [MaxLength(500)]
        public string? InstalledComponent { get; set; }
        
        [MaxLength(1000)]
        public string? Reason { get; set; }
        
        // Before Software Information
        [MaxLength(200)]
        public string? BeforeSwManufacturer { get; set; }
        
        [MaxLength(200)]
        public string? BeforeSwName { get; set; }
        
        [MaxLength(50)]
        public string? BeforeSwVersion { get; set; }
        
        // After Software Information
        [MaxLength(200)]
        public string? AfterSwManufacturer { get; set; }
        
        [MaxLength(200)]
        public string? AfterSwName { get; set; }
        
        [MaxLength(50)]
        public string? AfterSwVersion { get; set; }
        
        [MaxLength(2000)]
        public string? WorkDescription { get; set; }
        
        [MaxLength(2000)]
        public string? SecurityReviewComment { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Draft";
        
        // Approval workflow fields
        public int? PreparedByUserId { get; set; }
        public User? PreparedByUser { get; set; }
        
        public int? ReviewedByUserId { get; set; }
        public User? ReviewedByUser { get; set; }
        
        public int? ApprovedByUserId { get; set; }
        public User? ApprovedByUser { get; set; }
        
        public DateTime? PreparedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // Computed properties
        public string SoftwareChangeSummary => $"From: {BeforeSwName} v{BeforeSwVersion} → To: {AfterSwName} v{AfterSwVersion}";
        public string StatusDisplayName => Status switch
        {
            "Draft" => "Draft",
            "Submitted" => "Submitted",
            "Under Review" => "Under Review",
            "Approved" => "Approved",
            "Rejected" => "Rejected",
            _ => Status
        };
    }
} 