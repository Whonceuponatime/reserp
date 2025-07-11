using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User?> GetCurrentUserAsync();
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task LogoutAsync();
        Task<bool> HasPermissionAsync(string permission);
        Task<bool> IsInRoleAsync(string roleName);
        Task<bool> CanEditDataAsync();
        Task<bool> CanManageUsersAsync();
        Task<bool> CanApproveFormsAsync();
        Task<bool> CanSubmitFormsAsync();
        bool IsAuthenticated { get; }
        User? CurrentUser { get; }
    }
} 