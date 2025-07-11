using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class LoginLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // LOGIN, LOGOUT, LOGIN_FAILED, PASSWORD_RESET

        public bool IsSuccessful { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(200)]
        public string? Device { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? AdditionalInfo { get; set; }

        // Security fields
        public int? SessionDurationMinutes { get; set; }
        public bool IsSecurityEvent { get; set; } = false; // Flag for suspicious activities

        // Display properties
        public string ActionDisplay => Action switch
        {
            "LOGIN" => IsSuccessful ? "Login Successful" : "Login Failed",
            "LOGOUT" => "Logout",
            "LOGIN_FAILED" => "Login Failed",
            "PASSWORD_RESET" => "Password Reset",
            "ACCOUNT_LOCKED" => "Account Locked",
            "ACCOUNT_UNLOCKED" => "Account Unlocked",
            "PASSWORD_CHANGED" => "Password Changed",
            _ => Action
        };

        public string TimestampDisplay => Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        
        public string UserDisplay => User?.FullName ?? Username;
        
        public string StatusDisplay => IsSuccessful ? "Success" : "Failed";
        
        public string SecurityDisplay => IsSecurityEvent ? "⚠️ Security Event" : "Normal";

        public string DurationDisplay => SessionDurationMinutes.HasValue 
            ? $"{SessionDurationMinutes.Value} minutes" 
            : "N/A";
    }
} 