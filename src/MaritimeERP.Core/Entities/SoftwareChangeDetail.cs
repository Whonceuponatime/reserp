using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class SoftwareChangeDetail
    {
        public int Id { get; set; }

        // Foreign key for ChangeRequest (this makes SoftwareChangeDetail the dependent entity)
        public int ChangeRequestId { get; set; }
        public ChangeRequest ChangeRequest { get; set; } = null!;

        [MaxLength(200)]
        public string? PreSoftwareName { get; set; }

        [MaxLength(50)]
        public string? PreSoftwareVersion { get; set; }

        [MaxLength(200)]
        public string? PostSoftwareName { get; set; }

        [MaxLength(50)]
        public string? PostSoftwareVersion { get; set; }

        [MaxLength(2000)]
        public string? WorkDetails { get; set; }

        // Display properties
        public string ChangesSummary => $"From: {PreSoftwareName} v{PreSoftwareVersion} → To: {PostSoftwareName} v{PostSoftwareVersion}";
        public string VersionChangesSummary => $"Version: {PreSoftwareVersion} → {PostSoftwareVersion}";
    }
} 