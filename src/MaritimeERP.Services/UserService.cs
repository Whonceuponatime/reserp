using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;
using BCrypt.Net;

namespace MaritimeERP.Services
{
    public class UserService : IUserService
    {
        private readonly MaritimeERPContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(MaritimeERPContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID {UserId}", id);
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username {Username}", username);
                throw;
            }
        }

        public async Task<User> CreateUserAsync(User user, string password)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(user.Username))
                    throw new ArgumentException("Username is required");
                
                if (string.IsNullOrWhiteSpace(user.Email))
                    throw new ArgumentException("Email is required");
                
                if (string.IsNullOrWhiteSpace(user.FullName))
                    throw new ArgumentException("Full name is required");
                
                if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                    throw new ArgumentException("Password must be at least 8 characters long");

                // Check if username already exists
                if (await UsernameExistsAsync(user.Username))
                    throw new InvalidOperationException($"Username '{user.Username}' already exists");

                // Check if email already exists
                if (await EmailExistsAsync(user.Email))
                    throw new InvalidOperationException($"Email '{user.Email}' already exists");

                // Hash the password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully: {Username}", user.Username);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", user.Username);
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(user.Id);
                if (existingUser == null)
                    throw new InvalidOperationException($"User with ID {user.Id} not found");

                // Check if username is being changed and if it already exists
                if (existingUser.Username != user.Username && await UsernameExistsAsync(user.Username))
                    throw new InvalidOperationException($"Username '{user.Username}' already exists");

                // Check if email is being changed and if it already exists
                if (existingUser.Email != user.Email && await EmailExistsAsync(user.Email))
                    throw new InvalidOperationException($"Email '{user.Email}' already exists");

                existingUser.Username = user.Username;
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.RoleId = user.RoleId;
                existingUser.IsActive = user.IsActive;
                existingUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User updated successfully: {Username}", user.Username);
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", user.Id);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return false;

                // Don't allow deleting the admin user
                if (user.Username == "admin")
                    throw new InvalidOperationException("Cannot delete the admin user");

                // Instead of hard delete, we'll soft delete by deactivating
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User deactivated (soft deleted): {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                throw;
            }
        }

        public async Task<bool> ActivateUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return false;

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User activated: {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return false;

                // Don't allow deactivating the admin user
                if (user.Username == "admin")
                    throw new InvalidOperationException("Cannot deactivate the admin user");

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User deactivated: {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(int id, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                    throw new ArgumentException("Password must be at least 8 characters long");

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return false;

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset for user: {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", id);
                throw;
            }
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            try
            {
                return await _context.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all roles");
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if username exists: {Username}", username);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email exists: {Email}", email);
                throw;
            }
        }
    }
} 