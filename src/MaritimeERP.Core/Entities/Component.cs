using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaritimeERP.Core.Entities
{
    [Table("hardware_components")]
    public class Component
    {
        [Key]
        public int Id { get; set; }

        public int SystemId { get; set; }
        public ShipSystem System { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        [Column("system_name")]
        public string SystemName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("component_type")]
        public string ComponentType { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Column("component_name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        [Column("manufacturer")]
        public string? Manufacturer { get; set; }

        [MaxLength(200)]
        [Column("model")]
        public string? Model { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("installed_location")]
        public string InstalledLocation { get; set; } = string.Empty;

        [MaxLength(200)]
        [Column("os_name")]
        public string? OsName { get; set; }

        [MaxLength(50)]
        [Column("os_version")]
        public string? OsVersion { get; set; }

        [Column("lan_ports")]
        public short LanPorts { get; set; }

        [Column("usb_ports")]
        public short UsbPorts { get; set; }

        [MaxLength(200)]
        [Column("supported_protocols")]
        public string? SupportedProtocols { get; set; }

        [MaxLength(100)]
        [Column("network_segment")]
        public string? NetworkSegment { get; set; }

        [MaxLength(500)]
        [Column("connected_cbs")]
        public string? ConnectedCbs { get; set; }

        [MaxLength(500)]
        [Column("connection_purpose")]
        public string? ConnectionPurpose { get; set; }

        [Column("remote_connection")]
        public bool HasRemoteConnection { get; set; }

        [Column("type_approved")]
        public bool IsTypeApproved { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Software> Software { get; set; } = new List<Software>();

        // Display properties
        public string DisplayName => $"{Name} ({Manufacturer} {Model})".TrimEnd(' ', '(', ')');
        public string PortsInfo => $"USB: {UsbPorts}, LAN: {LanPorts}";
        public string LocationInfo => $"Location: {InstalledLocation}";
        public string MakerModel => $"{Manufacturer} {Model}".Trim();
        public string SerialPorts => $"Serial Ports: {UsbPorts}";
    }
} 