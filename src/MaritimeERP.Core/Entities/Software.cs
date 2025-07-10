using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaritimeERP.Core.Entities
{
    [Table("software_components")]
    public class Software
    {
        [Key]
        public int Id { get; set; }
        
        [Column("manufacturer")]
        [MaxLength(200)]
        public string? Manufacturer { get; set; }
        
        [Required]
        [Column("software_name")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Column("software_type")]
        [MaxLength(100)]
        public string? SoftwareType { get; set; }
        
        [Column("software_version")]
        [MaxLength(50)]
        public string? Version { get; set; }
        
        [Column("function_purpose")]
        [MaxLength(500)]
        public string? FunctionPurpose { get; set; }
        
        [Column("installed_hw_component")]
        [MaxLength(200)]
        public string? InstalledHardwareComponent { get; set; }

        [Column("installed_component_id")]
        public int? InstalledComponentId { get; set; }
        
        [ForeignKey("InstalledComponentId")]
        public virtual Component? InstalledComponent { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Display properties
        public string DisplayName => $"{Name} v{Version}".TrimEnd(' ', 'v');
        public string ManufacturerInfo => $"{Manufacturer ?? "Unknown"}";
        public string TypeInfo => $"{SoftwareType ?? "Unknown"}";
        
        // Get system name through the component relationship
        public string SystemName => InstalledComponent?.SystemName ?? "Unknown";

        [Column("license_type")]
        [MaxLength(100)]
        public string LicenseType { get; set; } = string.Empty;

        [Column("license_key")]
        [MaxLength(200)]
        public string LicenseKey { get; set; } = string.Empty;

        [Column("description")]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column("installation_date")]
        public DateTime? InstallationDate { get; set; }

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }
    }
} 