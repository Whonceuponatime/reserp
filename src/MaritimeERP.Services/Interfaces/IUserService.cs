using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User> CreateUserAsync(User user, string password);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> ActivateUserAsync(int id);
        Task<bool> DeactivateUserAsync(int id);
        Task<bool> ResetPasswordAsync(int id, string newPassword);
        Task<List<Role>> GetAllRolesAsync();
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
    }
} 