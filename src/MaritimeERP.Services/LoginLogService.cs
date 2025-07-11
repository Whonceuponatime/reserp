using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Services
{
    public class LoginLogService : ILoginLogService
    {
        private readonly MaritimeERPContext _context;
        private readonly ILogger<LoginLogService> _logger;

        public LoginLogService(MaritimeERPContext context, ILogger<LoginLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Login Log CRUD Operations

        public async Task<List<LoginLog>> GetAllLoginLogsAsync()
        {
            try
            {
                return await _context.LoginLogs
                    .Include(l => l.User)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all login logs");
                throw;
            }
        }

        public async Task<List<LoginLog>> GetLoginLogsByUserIdAsync(int userId)
        {
            try
            {
                return await _context.LoginLogs
                    .Include(l => l.User)
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving login logs for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<LoginLog>> GetLoginLogsByUsernameAsync(string username)
        {
            try
            {
                return await _context.LoginLogs
                    .Include(l => l.User)
                    .Where(l => l.Username == username)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving login logs for username {Username}", username);
                throw;
            }
        }

        public async Task<LoginLog?> GetLoginLogByIdAsync(int id)
        {
            try
            {
                return await _context.LoginLogs
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving login log {LoginLogId}", id);
                throw;
            }
        }

        #endregion

        #region Log Creation Methods

        public async Task LogSuccessfulLoginAsync(int userId, string username, string? ipAddress = null, string? userAgent = null, string? device = null)
        {
            try
            {
                var loginLog = new LoginLog
                {
                    UserId = userId,
                    Username = username,
                    Action = "LOGIN",
                    IsSuccessful = true,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Device = device,
                    Timestamp = DateTime.UtcNow
                };

                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successful login logged for user {Username} from {IpAddress}", username, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging successful login for user {Username}", username);
            }
        }

        public async Task LogFailedLoginAsync(string username, string failureReason, string? ipAddress = null, string? userAgent = null, string? device = null)
        {
            try
            {
                var loginLog = new LoginLog
                {
                    Username = username,
                    Action = "LOGIN_FAILED",
                    IsSuccessful = false,
                    FailureReason = failureReason,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Device = device,
                    Timestamp = DateTime.UtcNow,
                    IsSecurityEvent = true
                };

                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Failed login attempt logged for username {Username} from {IpAddress}: {FailureReason}", 
                    username, ipAddress, failureReason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging failed login for username {Username}", username);
            }
        }

        public async Task LogLogoutAsync(int userId, string username, int sessionDurationMinutes, string? ipAddress = null)
        {
            try
            {
                var loginLog = new LoginLog
                {
                    UserId = userId,
                    Username = username,
                    Action = "LOGOUT",
                    IsSuccessful = true,
                    IpAddress = ipAddress,
                    SessionDurationMinutes = sessionDurationMinutes,
                    Timestamp = DateTime.UtcNow
                };

                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Logout logged for user {Username} after {SessionDuration} minutes", 
                    username, sessionDurationMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging logout for user {Username}", username);
            }
        }

        public async Task LogPasswordResetAsync(int userId, string username, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var loginLog = new LoginLog
                {
                    UserId = userId,
                    Username = username,
                    Action = "PASSWORD_RESET",
                    IsSuccessful = true,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Timestamp = DateTime.UtcNow,
                    IsSecurityEvent = true
                };

                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset logged for user {Username} from {IpAddress}", username, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging password reset for user {Username}", username);
            }
        }

        public async Task LogPasswordChangeAsync(int userId, string username, string? ipAddress = null)
        {
            try
            {
                var loginLog = new LoginLog
                {
                    UserId = userId,
                    Username = username,
                    Action = "PASSWORD_CHANGED",
                    IsSuccessful = true,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password change logged for user {Username}", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging password change for user {Username}", username);
            }
        }

        public async Task LogAccountLockedAsync(int userId, string username, string reason, string? ipAddress = null)
        {
            try
            {
                var loginLog = new LoginLog
                {
                    UserId = userId,
                    Username = username,
                    Action = "ACCOUNT_LOCKED",
                    IsSuccessful = false,
                    FailureReason = reason,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    IsSecurityEvent = true
                };

                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Account locked logged for user {Username}: {Reason}", username, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging account lock for user {Username}", username);
            }
        }

        public async Task LogAccountUnlockedAsync(int userId, string username, string? ipAddress = null)
        {
            try
            {
                var loginLog = new LoginLog
                {
                    UserId = userId,
                    Username = username,
                    Action = "ACCOUNT_UNLOCKED",
                    IsSuccessful = true,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Account unlocked logged for user {Username}", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging account unlock for user {Username}", username);
            }
        }

        public async Task LogSecurityEventAsync(int? userId, string username, string action, string details, string? ipAddress = null)
        {
            try
            {
                var loginLog = new LoginLog
                {
                    UserId = userId,
                    Username = username,
                    Action = action,
                    IsSuccessful = false,
                    FailureReason = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    IsSecurityEvent = true
                };

                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Security event logged for user {Username}: {Action} - {Details}", username, action, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event for user {Username}", username);
            }
        }

        #endregion

        #region Filtering and Search

        public async Task<List<LoginLog>> GetLoginLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.LoginLogs
                    .Include(l => l.User)
                    .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving login logs by date range");
                throw;
            }
        }

        public async Task<List<LoginLog>> GetFailedLoginAttemptsAsync(int hours = 24)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-hours);
                return await _context.LoginLogs
                    .Include(l => l.User)
                    .Where(l => !l.IsSuccessful && l.Timestamp >= cutoffTime)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving failed login attempts");
                throw;
            }
        }

        public async Task<List<LoginLog>> GetSecurityEventsAsync(int days = 7)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddDays(-days);
                return await _context.LoginLogs
                    .Include(l => l.User)
                    .Where(l => l.IsSecurityEvent && l.Timestamp >= cutoffTime)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security events");
                throw;
            }
        }

        public async Task<List<LoginLog>> GetFilteredLoginLogsAsync(string? username = null, string? action = null, 
            bool? isSuccessful = null, bool? isSecurityEvent = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.LoginLogs.Include(l => l.User).AsQueryable();

                if (!string.IsNullOrEmpty(username))
                    query = query.Where(l => l.Username.Contains(username));

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(l => l.Action == action);

                if (isSuccessful.HasValue)
                    query = query.Where(l => l.IsSuccessful == isSuccessful.Value);

                if (isSecurityEvent.HasValue)
                    query = query.Where(l => l.IsSecurityEvent == isSecurityEvent.Value);

                if (startDate.HasValue)
                    query = query.Where(l => l.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(l => l.Timestamp <= endDate.Value);

                return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered login logs");
                throw;
            }
        }

        #endregion

        #region Statistics and Analytics

        public async Task<Dictionary<string, int>> GetLoginStatsByAction()
        {
            try
            {
                return await _context.LoginLogs
                    .GroupBy(l => l.Action)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting login statistics by action");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetLoginStatsByUser(int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await _context.LoginLogs
                    .Where(l => l.Timestamp >= cutoffDate)
                    .GroupBy(l => l.Username)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting login statistics by user");
                throw;
            }
        }

        public async Task<Dictionary<DateTime, int>> GetLoginTrendsByDay(int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await _context.LoginLogs
                    .Where(l => l.Timestamp >= cutoffDate && l.Action == "LOGIN")
                    .GroupBy(l => l.Timestamp.Date)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting login trends by day");
                throw;
            }
        }

        public async Task<List<string>> GetTopFailedLoginUsernamesAsync(int count = 10, int hours = 24)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-hours);
                return await _context.LoginLogs
                    .Where(l => !l.IsSuccessful && l.Timestamp >= cutoffTime)
                    .GroupBy(l => l.Username)
                    .OrderByDescending(g => g.Count())
                    .Take(count)
                    .Select(g => g.Key)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top failed login usernames");
                throw;
            }
        }

        public async Task<int> GetFailedLoginCountAsync(string username, int hours = 1)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-hours);
                return await _context.LoginLogs
                    .CountAsync(l => l.Username == username && !l.IsSuccessful && l.Timestamp >= cutoffTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failed login count for user {Username}", username);
                throw;
            }
        }

        public async Task<int> GetTotalLoginAttemptsAsync(int days = 1)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await _context.LoginLogs
                    .CountAsync(l => l.Action == "LOGIN" && l.Timestamp >= cutoffDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total login attempts");
                throw;
            }
        }

        public async Task<int> GetSuccessfulLoginsAsync(int days = 1)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await _context.LoginLogs
                    .CountAsync(l => l.Action == "LOGIN" && l.IsSuccessful && l.Timestamp >= cutoffDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting successful logins");
                throw;
            }
        }

        public async Task<double> GetLoginSuccessRateAsync(int days = 7)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var totalAttempts = await _context.LoginLogs
                    .CountAsync(l => l.Action == "LOGIN" && l.Timestamp >= cutoffDate);
                
                if (totalAttempts == 0) return 0;

                var successfulLogins = await _context.LoginLogs
                    .CountAsync(l => l.Action == "LOGIN" && l.IsSuccessful && l.Timestamp >= cutoffDate);

                return (double)successfulLogins / totalAttempts * 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating login success rate");
                throw;
            }
        }

        #endregion

        #region Security Monitoring

        public async Task<List<LoginLog>> GetSuspiciousActivitiesAsync(int days = 7)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddDays(-days);
                return await _context.LoginLogs
                    .Include(l => l.User)
                    .Where(l => l.IsSecurityEvent && l.Timestamp >= cutoffTime)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suspicious activities");
                throw;
            }
        }

        public async Task<bool> IsUserLoginSuspiciousAsync(string username, string ipAddress)
        {
            try
            {
                var recentFailures = await GetFailedLoginCountAsync(username, 1);
                
                // Consider suspicious if more than 3 failed attempts in the last hour
                if (recentFailures > 3)
                {
                    return true;
                }

                // Check for unusual IP address
                var recentLogins = await _context.LoginLogs
                    .Where(l => l.Username == username && l.IsSuccessful && l.Timestamp >= DateTime.UtcNow.AddDays(-30))
                    .Select(l => l.IpAddress)
                    .Distinct()
                    .ToListAsync();

                // If this is a new IP address for the user, it might be suspicious
                return !recentLogins.Contains(ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user login is suspicious for {Username}", username);
                return false;
            }
        }

        public async Task<List<string>> GetUniqueIpAddressesAsync(string username, int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await _context.LoginLogs
                    .Where(l => l.Username == username && l.Timestamp >= cutoffDate && !string.IsNullOrEmpty(l.IpAddress))
                    .Select(l => l.IpAddress!)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unique IP addresses for user {Username}", username);
                throw;
            }
        }

        public async Task<DateTime?> GetLastSuccessfulLoginAsync(int userId)
        {
            try
            {
                var lastLogin = await _context.LoginLogs
                    .Where(l => l.UserId == userId && l.Action == "LOGIN" && l.IsSuccessful)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();

                return lastLogin?.Timestamp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last successful login for user {UserId}", userId);
                throw;
            }
        }

        public async Task<DateTime?> GetLastFailedLoginAsync(string username)
        {
            try
            {
                var lastFailedLogin = await _context.LoginLogs
                    .Where(l => l.Username == username && !l.IsSuccessful)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();

                return lastFailedLogin?.Timestamp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last failed login for username {Username}", username);
                throw;
            }
        }

        #endregion

        #region Cleanup and Maintenance

        public async Task<int> CleanupOldLogsAsync(int daysToKeep = 365)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var oldLogs = await _context.LoginLogs
                    .Where(l => l.Timestamp < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _context.LoginLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Cleaned up {Count} old login logs older than {CutoffDate}", 
                        oldLogs.Count, cutoffDate);
                }

                return oldLogs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old login logs");
                throw;
            }
        }

        public async Task<bool> DeleteLoginLogAsync(int id)
        {
            try
            {
                var loginLog = await _context.LoginLogs.FindAsync(id);
                if (loginLog == null)
                {
                    return false;
                }

                _context.LoginLogs.Remove(loginLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Login log {LoginLogId} deleted", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting login log {LoginLogId}", id);
                throw;
            }
        }

        #endregion
    }
} 