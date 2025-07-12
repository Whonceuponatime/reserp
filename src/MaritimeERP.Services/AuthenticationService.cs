using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace MaritimeERP.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuthenticationService> _logger;
        private User? _currentUser;
        private bool _databaseInitialized = false;
        private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

        public AuthenticationService(IServiceProvider serviceProvider, ILogger<AuthenticationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public bool IsAuthenticated => _currentUser != null;
        public User? CurrentUser => _currentUser;

        private async Task EnsureDatabaseInitializedAsync()
        {
            if (_databaseInitialized)
                return;

            await _initSemaphore.WaitAsync();
            try
            {
                if (_databaseInitialized)
                    return;

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<MaritimeERPContext>();
                    
                    // Ensure database is created
                    await context.Database.EnsureCreatedAsync();
                    
                    // Check if admin user exists
                    var adminExists = await context.Users.AnyAsync(u => u.Username == "admin");
                    
                    if (!adminExists)
                    {
                        // Create admin user
                        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
                        if (adminRole == null)
                        {
                            adminRole = new Role { Name = "Administrator", Description = "Full system access" };
                            context.Roles.Add(adminRole);
                            await context.SaveChangesAsync();
                            _logger.LogInformation("Created Administrator role with ID: {RoleId}", adminRole.Id);
                        }
                        else
                        {
                            _logger.LogInformation("Found existing Administrator role with ID: {RoleId}", adminRole.Id);
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

                        context.Users.Add(adminUser);
                        await context.SaveChangesAsync();
                        _logger.LogInformation("Admin user created successfully with RoleId: {RoleId} (Role: {RoleName})", 
                            adminRole.Id, adminRole.Name);
                    }
                    else
                    {
                        // Check if existing admin user has correct role
                        var existingAdmin = await context.Users
                            .Include(u => u.Role)
                            .FirstOrDefaultAsync(u => u.Username == "admin");
                        
                        if (existingAdmin != null)
                        {
                            _logger.LogInformation("Existing admin user found - RoleId: {RoleId}, RoleName: {RoleName}", 
                                existingAdmin.RoleId, existingAdmin.Role?.Name ?? "NULL");
                                
                            // If admin user doesn't have Administrator role, fix it
                            if (existingAdmin.Role?.Name != "Administrator")
                            {
                                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
                                if (adminRole != null)
                                {
                                    existingAdmin.RoleId = adminRole.Id;
                                    await context.SaveChangesAsync();
                                    _logger.LogInformation("Fixed admin user role assignment to Administrator (RoleId: {RoleId})", adminRole.Id);
                                }
                            }
                        }
                    }

                    _databaseInitialized = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing database");
                    throw;
                }
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Authentication attempted with empty username or password");
                    
                    // Log failed login attempt
                    using var logScope = _serviceProvider.CreateScope();
                    var loginLogService = logScope.ServiceProvider.GetRequiredService<ILoginLogService>();
                    await loginLogService.LogFailedLoginAsync(username ?? "empty", "Empty username or password");
                    
                    return null;
                }

                // Ensure database is initialized
                await EnsureDatabaseInitializedAsync();

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaritimeERPContext>();

                var user = await context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Authentication failed: User {Username} not found or inactive", username);
                    
                    // Log failed login attempt
                    var loginLogService = scope.ServiceProvider.GetRequiredService<ILoginLogService>();
                    await loginLogService.LogFailedLoginAsync(username, "User not found or inactive");
                    
                    return null;
                }

                // Debug logging for role information
                _logger.LogInformation("User {Username} found - ID: {UserId}, RoleId: {RoleId}, RoleName: {RoleName}", 
                    user.Username, user.Id, user.RoleId, user.Role?.Name ?? "NULL");

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
                    
                    // Log failed login attempt
                    var loginLogService = scope.ServiceProvider.GetRequiredService<ILoginLogService>();
                    await loginLogService.LogFailedLoginAsync(username, "Invalid password");
                    
                    return null;
                }

                // Update last login in a separate scope to avoid locking
                await Task.Run(async () =>
                {
                    try
                    {
                        using var updateScope = _serviceProvider.CreateScope();
                        var updateContext = updateScope.ServiceProvider.GetRequiredService<MaritimeERPContext>();
                        
                        var userToUpdate = await updateContext.Users.FindAsync(user.Id);
                        if (userToUpdate != null)
                        {
                            userToUpdate.LastLoginAt = DateTime.UtcNow;
                            userToUpdate.UpdatedAt = DateTime.UtcNow;
                            await updateContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update last login time for user {Username}", username);
                        // Don't fail authentication if we can't update last login
                    }
                });

                // Log successful login
                using var successLogScope = _serviceProvider.CreateScope();
                var successLoginLogService = successLogScope.ServiceProvider.GetRequiredService<ILoginLogService>();
                await successLoginLogService.LogSuccessfulLoginAsync(user.Id, username);

                // Ensure user has role information loaded
                if (user.Role == null)
                {
                    _logger.LogWarning("User {Username} authenticated but role is null, reloading user", username);
                    var userWithRole = await context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == user.Id);
                    if (userWithRole != null)
                    {
                        user = userWithRole;
                        _logger.LogInformation("Reloaded user {Username} with role {RoleName}", username, user.Role?.Name ?? "NULL");
                    }
                }

                _currentUser = user;
                _logger.LogInformation("User {Username} authenticated successfully with role {RoleName}", username, user.Role?.Name ?? "NULL");
                _logger.LogInformation("_currentUser set - Username: {Username}, Role: {RoleName}", _currentUser.Username, _currentUser.Role?.Name ?? "NULL");
                
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for user {Username}", username);
                
                // Log failed login attempt due to system error
                try
                {
                    using var errorLogScope = _serviceProvider.CreateScope();
                    var errorLoginLogService = errorLogScope.ServiceProvider.GetRequiredService<ILoginLogService>();
                    await errorLoginLogService.LogFailedLoginAsync(username, $"System error: {ex.Message}");
                }
                catch
                {
                    // Ignore logging errors
                }
                
                return null;
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            _logger.LogInformation("GetCurrentUserAsync called - _currentUser is {IsNull}", _currentUser == null ? "NULL" : "NOT NULL");
            
            if (_currentUser == null)
            {
                _logger.LogWarning("GetCurrentUserAsync: _currentUser is null, returning null");
                return null;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaritimeERPContext>();
                
                _logger.LogInformation("GetCurrentUserAsync: Looking for user ID {UserId}", _currentUser.Id);
                
                // Refresh user data from database
                var user = await context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == _currentUser.Id && u.IsActive);

                if (user != null)
                {
                    _logger.LogInformation("GetCurrentUserAsync: Found user {Username} with RoleId {RoleId}, RoleName {RoleName}", 
                        user.Username, user.RoleId, user.Role?.Name ?? "NULL");
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
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaritimeERPContext>();
                
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Password change failed: User {UserId} not found", userId);
                    return false;
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", userId);
                    
                    // Log failed password change attempt
                    var loginLogService = scope.ServiceProvider.GetRequiredService<ILoginLogService>();
                    await loginLogService.LogSecurityEventAsync(userId, user.Username, "PASSWORD_CHANGE_FAILED", "Invalid current password");
                    
                    return false;
                }

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                {
                    _logger.LogWarning("Password change failed: New password too weak for user {UserId}", userId);
                    
                    // Log failed password change attempt
                    var loginLogService = scope.ServiceProvider.GetRequiredService<ILoginLogService>();
                    await loginLogService.LogSecurityEventAsync(userId, user.Username, "PASSWORD_CHANGE_FAILED", "New password too weak");
                    
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                // Log successful password change
                var successLoginLogService = scope.ServiceProvider.GetRequiredService<ILoginLogService>();
                await successLoginLogService.LogPasswordChangeAsync(userId, user.Username);

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            if (_currentUser != null)
            {
                _logger.LogInformation("User {Username} logged out", _currentUser.Username);
                
                // Log logout event
                try
                {
                    using var logScope = _serviceProvider.CreateScope();
                    var loginLogService = logScope.ServiceProvider.GetRequiredService<ILoginLogService>();
                    
                    // Calculate session duration if possible
                    var sessionDuration = _currentUser.LastLoginAt.HasValue 
                        ? (int)(DateTime.UtcNow - _currentUser.LastLoginAt.Value).TotalMinutes
                        : 0;
                    
                    await loginLogService.LogLogoutAsync(_currentUser.Id, _currentUser.Username, sessionDuration);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log logout for user {Username}", _currentUser.Username);
                }
                
                _currentUser = null;
            }
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