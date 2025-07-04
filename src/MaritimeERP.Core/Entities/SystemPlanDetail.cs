using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class SystemPlanDetail
    {
        public int Id { get; set; }

        // Foreign key for ChangeRequest (this makes SystemPlanDetail the dependent entity)
        public int ChangeRequestId { get; set; }
        public ChangeRequest ChangeRequest { get; set; } = null!;

        [MaxLength(5000)]
        public string? PlanDetails { get; set; }

        // Display properties
        public string PlanSummary => PlanDetails?.Length > 100 ? $"{PlanDetails[..100]}..." : PlanDetails ?? "";
    }
} 