using System;
using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class SystemChangePlan
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string RequestNumber { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Status tracking
        public bool IsCreated { get; set; } = true;
        public bool IsUnderReview { get; set; } = false;
        public bool IsApproved { get; set; } = false;
        
        // Requester Information
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string PositionTitle { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string RequesterName { get; set; } = string.Empty;
        
        // Installation Details
        [StringLength(200)]
        public string InstalledCbs { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string InstalledComponent { get; set; } = string.Empty;
        
        // Change Reason
        public string Reason { get; set; } = string.Empty;
        
        // Before Change Details
        [StringLength(200)]
        public string BeforeManufacturerModel { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string BeforeHwSwName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string BeforeVersion { get; set; } = string.Empty;
        
        // After Change Details
        [StringLength(200)]
        public string AfterManufacturerModel { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string AfterHwSwName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string AfterVersion { get; set; } = string.Empty;
        
        // Plan Details
        public string PlanDetails { get; set; } = string.Empty;
        
        // Security Review
        public string SecurityReviewComments { get; set; } = string.Empty;
        
        // Navigation properties
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }
} 