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
        public DateTime? ReviewDate { get; set; }
        
        // Reviewer information (검토자)
        [StringLength(100)]
        public string ReviewerDepartment { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string ReviewerPosition { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string ReviewerName { get; set; } = string.Empty;
        
        // Review items (검토 항목) - Individual items
        public string ReviewItem1 { get; set; } = string.Empty;
        public string ReviewResult1 { get; set; } = string.Empty;
        public string ReviewRemarks1 { get; set; } = string.Empty;
        
        public string ReviewItem2 { get; set; } = string.Empty;
        public string ReviewResult2 { get; set; } = string.Empty;
        public string ReviewRemarks2 { get; set; } = string.Empty;
        
        public string ReviewItem3 { get; set; } = string.Empty;
        public string ReviewResult3 { get; set; } = string.Empty;
        public string ReviewRemarks3 { get; set; } = string.Empty;
        
        public string ReviewItem4 { get; set; } = string.Empty;
        public string ReviewResult4 { get; set; } = string.Empty;
        public string ReviewRemarks4 { get; set; } = string.Empty;
        
        public string ReviewItem5 { get; set; } = string.Empty;
        public string ReviewResult5 { get; set; } = string.Empty;
        public string ReviewRemarks5 { get; set; } = string.Empty;
        
        // Overall review results (검토 결과)
        public string OverallReviewResult { get; set; } = string.Empty;
        
        // Review opinion (검토 의견)
        public string ReviewOpinion { get; set; } = string.Empty;
        
        // Navigation properties
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        
        // Display properties
        public string StatusDisplay
        {
            get
            {
                if (IsApproved) return "Approved";
                if (IsUnderReview) return "Under Review";
                return "Draft";
            }
        }
    }
} 