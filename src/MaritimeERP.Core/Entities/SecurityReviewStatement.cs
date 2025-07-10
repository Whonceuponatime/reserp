using System;
using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class SecurityReviewStatement
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string RequestNumber { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Ship association
        public int? ShipId { get; set; }
        public Ship? Ship { get; set; }
        
        // Status tracking (작성/검토/승인)
        public bool IsCreated { get; set; } = true;
        public bool IsUnderReview { get; set; } = false;
        public bool IsApproved { get; set; } = false;
        
        // Request information (등록요청 번호, 작성 일자)
        public DateTime ReviewDate { get; set; } = DateTime.Now;
        
        // Reviewer information (검토자)
        [StringLength(100)]
        public string ReviewerDepartment { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string ReviewerPosition { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string ReviewerName { get; set; } = string.Empty;
        
        // Review sections (구분 - 검토 항목/검토 결과/비고)
        // We'll store these as structured text fields for simplicity
        public string ReviewItems { get; set; } = string.Empty; // JSON or structured format
        public string ReviewResults { get; set; } = string.Empty; // JSON or structured format
        public string ReviewNotes { get; set; } = string.Empty; // JSON or structured format
        
        // Overall review results (검토 결과)
        public string OverallResult { get; set; } = string.Empty;
        
        // Review opinion (검토 의견)
        public string ReviewOpinion { get; set; } = string.Empty;
        
        // Navigation properties
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }
} 