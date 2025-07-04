using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<ChangeRequest> ChangeRequests { get; set; } = new List<ChangeRequest>();
        public ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();
        public ICollection<Approval> Approvals { get; set; } = new List<Approval>();

        // Display properties
        public string DisplayName => $"{FullName} ({Username})";
        public string StatusDisplay => IsActive ? "Active" : "Inactive";
        public string LastLoginDisplay => LastLoginAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never";
    }
} 