using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class Component
    {
        public int Id { get; set; }

        public int SystemId { get; set; }
        public ShipSystem System { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string MakerModel { get; set; } = string.Empty;

        public short UsbPorts { get; set; } = 0;
        public short LanPorts { get; set; } = 0;
        public short SerialPorts { get; set; } = 0;

        [MaxLength(500)]
        public string? ConnectedCbs { get; set; }

        public bool HasRemoteConnection { get; set; } = false;

        [Required]
        [MaxLength(200)]
        public string InstalledLocation { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Software> Software { get; set; } = new List<Software>();

        // Display properties
        public string DisplayName => $"{Name} ({MakerModel})";
        public string PortsInfo => $"USB: {UsbPorts}, LAN: {LanPorts}, Serial: {SerialPorts}";
        public string LocationInfo => $"Location: {InstalledLocation}";
    }
} 