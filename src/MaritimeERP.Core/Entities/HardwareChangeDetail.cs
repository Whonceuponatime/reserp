using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class HardwareChangeDetail
    {
        public int Id { get; set; }

        // Foreign key for ChangeRequest (this makes HardwareChangeDetail the dependent entity)
        public int ChangeRequestId { get; set; }
        public ChangeRequest ChangeRequest { get; set; } = null!;

        [MaxLength(200)]
        public string? PreHardwareModel { get; set; }

        [MaxLength(200)]
        public string? PreOperatingSystem { get; set; }

        [MaxLength(200)]
        public string? PostHardwareModel { get; set; }

        [MaxLength(200)]
        public string? PostOperatingSystem { get; set; }

        [MaxLength(2000)]
        public string? WorkDetails { get; set; }

        // Display properties
        public string ChangesSummary => $"From: {PreHardwareModel} → To: {PostHardwareModel}";
        public string OsChangesSummary => $"OS: {PreOperatingSystem} → {PostOperatingSystem}";
    }
} 