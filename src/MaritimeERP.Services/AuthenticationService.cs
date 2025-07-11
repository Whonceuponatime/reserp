using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly MaritimeERPContext _context;
        private readonly ILogger<AuthenticationService> _logger;
        private User? _currentUser;
        private bool _databaseInitialized = false;

        public AuthenticationService(MaritimeERPContext context, ILogger<AuthenticationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public bool IsAuthenticated => _currentUser != null;
        public User? CurrentUser => _currentUser;

        private async Task EnsureDatabaseInitializedAsync()
        {
            if (_databaseInitialized)
                return;

            try
            {
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();
                
                // Check if admin user exists
                var adminExists = await _context.Users.AnyAsync(u => u.Username == "admin");
                
                if (!adminExists)
                {
                    // Create admin user
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
                    if (adminRole == null)
                    {
                        adminRole = new Role { Name = "Administrator", Description = "Full system access" };
                        _context.Roles.Add(adminRole);
                        await _context.SaveChangesAsync();
                    }

                    var adminUser = new User
                    {
                        Username = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"), // Properly hashed password for "admin"
                        FullName = "System Administrator",
                        Email = "admin@maritime.com",
                        RoleId = adminRole.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Admin user created successfully");
                }

                _databaseInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Authentication attempted with empty username or password");
                    return null;
                }

                // Ensure database is initialized
                await EnsureDatabaseInitializedAsync();

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Authentication failed: User {Username} not found or inactive", username);
                    return null;
                }

                // Temporary safety bypass for admin - remove after setting proper password
                bool isValidPassword = false;
                
                if (username == "admin" && (password == "admin" || password == "admin123"))
                {
                    // Temporary bypass for admin user
                    isValidPassword = true;
                    _logger.LogInformation("Admin user logged in with temporary bypass");
                }
                else
                {
                    // Normal password verification
                    isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }

                if (!isValidPassword)
                {
                    _logger.LogWarning("Authentication failed: Invalid password for user {Username}", username);
                    return null;
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _currentUser = user;
                _logger.LogInformation("User {Username} authenticated successfully", username);
                
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for user {Username}", username);
                return null;
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            if (_currentUser == null)
                return null;

            try
            {
                // Refresh user data from database
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == _currentUser.Id && u.IsActive);

                if (user != null)
                {
                    _currentUser = user;
                }
                else
                {
                    _logger.LogWarning("Current user {UserId} no longer exists or is inactive", _currentUser.Id);
                    _currentUser = null;
                }

                return _currentUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing current user data");
                return _currentUser; // Return cached version on error
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Password change failed: User {UserId} not found", userId);
                    return false;
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", userId);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                {
                    _logger.LogWarning("Password change failed: New password too weak for user {UserId}", userId);
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return false;
            }
        }

        public Task LogoutAsync()
        {
            if (_currentUser != null)
            {
                _logger.LogInformation("User {Username} logged out", _currentUser.Username);
                _currentUser = null;
            }
            return Task.CompletedTask;
        }

        public async Task<bool> HasPermissionAsync(string permission)
        {
            var user = await GetCurrentUserAsync();
            if (user?.Role == null)
                return false;

            // Role-based permissions
            return user.Role.Name switch
            {
                "Administrator" => true, // Admin has all permissions
                "Engineer" => permission.StartsWith("Read") || permission.StartsWith("Submit") || permission.StartsWith("View"),
                _ => false // No permissions for any other roles
            };
        }

        public async Task<bool> CanEditDataAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Role?.Name == "Administrator";
        }

        public async Task<bool> CanManageUsersAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Role?.Name == "Administrator";
        }

        public async Task<bool> CanApproveFormsAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Role?.Name == "Administrator";
        }

        public async Task<bool> CanSubmitFormsAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Role?.Name == "Administrator" || user?.Role?.Name == "Engineer";
        }

        public async Task<bool> IsInRoleAsync(string roleName)
        {
            var user = await GetCurrentUserAsync();
            return user?.Role?.Name?.Equals(roleName, StringComparison.OrdinalIgnoreCase) == true;
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
} 