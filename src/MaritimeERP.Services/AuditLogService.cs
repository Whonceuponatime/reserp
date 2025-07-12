using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;
using System.Text.Json;
using System.Reflection;

namespace MaritimeERP.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly MaritimeERPContext _context;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            MaritimeERPContext context,
            IAuthenticationService authenticationService,
            ILogger<AuditLogService> logger)
        {
            _context = context;
            _authenticationService = authenticationService;
            _logger = logger;
        }

        public async Task<List<AuditLog>> GetAllAuditLogsAsync()
        {
            try
            {
                var auditLogs = await _context.AuditLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();

                // Include login logs as security audit entries
                var loginLogs = await GetLoginLogsAsAuditEntriesAsync();
                auditLogs.AddRange(loginLogs);

                return auditLogs.OrderByDescending(a => a.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all audit logs");
                throw;
            }
        }

        public async Task<List<AuditLog>> GetAuditLogsByEntityTypeAsync(string entityType)
        {
            try
            {
                if (entityType == "Security")
                {
                    // Return only login logs as security audit entries
                    return await GetLoginLogsAsAuditEntriesAsync();
                }

                return await _context.AuditLogs
                    .Include(a => a.User)
                    .Where(a => a.EntityType == entityType)
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs by entity type {EntityType}", entityType);
                throw;
            }
        }

        public async Task<List<AuditLog>> GetAuditLogsByActionAsync(string action)
        {
            try
            {
                return await _context.AuditLogs
                    .Include(a => a.User)
                    .Where(a => a.Action == action)
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs by action {Action}", action);
                throw;
            }
        }

        public async Task<List<AuditLog>> GetAuditLogsByUserAsync(int userId)
        {
            try
            {
                return await _context.AuditLogs
                    .Include(a => a.User)
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs by user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.AuditLogs
                    .Include(a => a.User)
                    .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs by date range {StartDate} - {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<List<AuditLog>> GetFilteredAuditLogsAsync(string? entityType = null, string? action = null, int? userId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.AuditLogs
                    .Include(a => a.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(entityType) && entityType != "Security")
                    query = query.Where(a => a.EntityType == entityType);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(a => a.Action == action);

                if (userId.HasValue)
                    query = query.Where(a => a.UserId == userId.Value);

                if (startDate.HasValue)
                    query = query.Where(a => a.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.Timestamp <= endDate.Value);

                var auditLogs = await query
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();

                // Include login logs if Security category is selected or no specific entity type is selected
                if (string.IsNullOrEmpty(entityType) || entityType == "Security")
                {
                    var loginLogs = await GetFilteredLoginLogsAsAuditEntriesAsync(action, userId, startDate, endDate);
                    auditLogs.AddRange(loginLogs);
                }

                return auditLogs.OrderByDescending(a => a.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered audit logs");
                throw;
            }
        }

        public async Task<List<string>> GetDistinctEntityTypesAsync()
        {
            try
            {
                var entityTypes = await _context.AuditLogs
                    .Select(a => a.EntityType)
                    .Distinct()
                    .OrderBy(et => et)
                    .ToListAsync();

                // Add Security category for login logs
                entityTypes.Add("Security");

                return entityTypes.OrderBy(et => et).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving distinct entity types");
                throw;
            }
        }

        public async Task<List<string>> GetDistinctActionsAsync()
        {
            try
            {
                return await _context.AuditLogs
                    .Select(a => a.Action)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving distinct actions");
                throw;
            }
        }

        public async Task LogAsync(string entityType, string action, string entityId, string? entityName = null, string? oldValues = null, string? newValues = null, string? additionalInfo = null)
        {
            try
            {
                var currentUser = await _authenticationService.GetCurrentUserAsync();
                var userName = currentUser?.FullName ?? "System";
                var userId = currentUser?.Id;

                _logger.LogInformation("Attempting to log audit entry: {EntityType} {Action} {EntityId} by user {UserName}", 
                    entityType, action, entityId, userName);
                
                var auditLog = new AuditLog
                {
                    EntityType = entityType,
                    Action = action,
                    EntityId = entityId,
                    EntityName = entityName,
                    OldValues = oldValues,
                    NewValues = newValues,
                    AdditionalInfo = additionalInfo,
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    UserName = userName
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully logged audit entry: {EntityType} {Action} {EntityId}", 
                    entityType, action, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL: Failed to log audit entry for {EntityType} {Action} {EntityId}. " +
                    "This means the audit trail is incomplete! Error: {ErrorMessage}", 
                    entityType, action, entityId, ex.Message);
                
                // Try to log with minimal information as fallback
                try
                {
                    var fallbackLog = new AuditLog
                    {
                        EntityType = entityType,
                        Action = action,
                        EntityId = entityId,
                        EntityName = entityName,
                        AdditionalInfo = $"Fallback log - Original error: {ex.Message}",
                        Timestamp = DateTime.UtcNow,
                        UserId = null,
                        UserName = "System (Error Recovery)"
                    };

                    _context.AuditLogs.Add(fallbackLog);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Created fallback audit log for {EntityType} {Action} {EntityId}", 
                        entityType, action, entityId);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "CRITICAL: Even fallback audit logging failed for {EntityType} {Action} {EntityId}", 
                        entityType, action, entityId);
                }
            }
        }

        public async Task LogCreateAsync<T>(T entity, string? additionalInfo = null) where T : class
        {
            try
            {
                var entityType = typeof(T).Name;
                var entityId = GetEntityId(entity);
                var entityName = GetEntityName(entity);
                var newValues = SerializeEntity(entity);

                _logger.LogDebug("LogCreateAsync called for {EntityType} with ID {EntityId} and name '{EntityName}'", 
                    entityType, entityId, entityName);

                await LogAsync(entityType, "CREATE", entityId, entityName, null, newValues, additionalInfo);
                
                _logger.LogDebug("LogCreateAsync completed successfully for {EntityType} {EntityId}", entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogCreateAsync for {EntityType}", typeof(T).Name);
                // Still try to log with minimal data
                try
                {
                    await LogAsync(typeof(T).Name, "CREATE", "unknown", null, null, null, 
                        $"Error in detailed logging: {ex.Message}. Additional: {additionalInfo}");
                }
                catch
                {
                    // Last resort logging
                    _logger.LogError("Complete failure to log CREATE action for {EntityType}", typeof(T).Name);
                }
            }
        }

        public async Task LogUpdateAsync<T>(T oldEntity, T newEntity, string? additionalInfo = null) where T : class
        {
            try
            {
                var entityType = typeof(T).Name;
                var entityId = GetEntityId(newEntity);
                var entityName = GetEntityName(newEntity);
                var oldValues = SerializeEntity(oldEntity);
                var newValues = SerializeEntity(newEntity);

                _logger.LogDebug("LogUpdateAsync called for {EntityType} with ID {EntityId} and name '{EntityName}'", 
                    entityType, entityId, entityName);

                await LogAsync(entityType, "UPDATE", entityId, entityName, oldValues, newValues, additionalInfo);
                
                _logger.LogDebug("LogUpdateAsync completed successfully for {EntityType} {EntityId}", entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogUpdateAsync for {EntityType}", typeof(T).Name);
                // Still try to log with minimal data
                try
                {
                    await LogAsync(typeof(T).Name, "UPDATE", "unknown", null, null, null, 
                        $"Error in detailed logging: {ex.Message}. Additional: {additionalInfo}");
                }
                catch
                {
                    // Last resort logging
                    _logger.LogError("Complete failure to log UPDATE action for {EntityType}", typeof(T).Name);
                }
            }
        }

        public async Task LogDeleteAsync<T>(T entity, string? additionalInfo = null) where T : class
        {
            try
            {
                var entityType = typeof(T).Name;
                var entityId = GetEntityId(entity);
                var entityName = GetEntityName(entity);
                var oldValues = SerializeEntity(entity);

                _logger.LogDebug("LogDeleteAsync called for {EntityType} with ID {EntityId} and name '{EntityName}'", 
                    entityType, entityId, entityName);

                await LogAsync(entityType, "DELETE", entityId, entityName, oldValues, null, additionalInfo);
                
                _logger.LogDebug("LogDeleteAsync completed successfully for {EntityType} {EntityId}", entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogDeleteAsync for {EntityType}", typeof(T).Name);
                // Still try to log with minimal data
                try
                {
                    await LogAsync(typeof(T).Name, "DELETE", "unknown", null, null, null, 
                        $"Error in detailed logging: {ex.Message}. Additional: {additionalInfo}");
                }
                catch
                {
                    // Last resort logging
                    _logger.LogError("Complete failure to log DELETE action for {EntityType}", typeof(T).Name);
                }
            }
        }

        public async Task LogActionAsync(string entityType, string action, string entityId, string? entityName = null, string? additionalInfo = null)
        {
            try
            {
                _logger.LogDebug("LogActionAsync called for {EntityType} {Action} with ID {EntityId} and name '{EntityName}'", 
                    entityType, action, entityId, entityName);

                await LogAsync(entityType, action, entityId, entityName, null, null, additionalInfo);
                
                _logger.LogDebug("LogActionAsync completed successfully for {EntityType} {Action} {EntityId}", 
                    entityType, action, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogActionAsync for {EntityType} {Action}", entityType, action);
                // Still try to log with minimal data
                try
                {
                    await LogAsync(entityType, action, entityId ?? "unknown", entityName, null, null, 
                        $"Error in detailed logging: {ex.Message}. Additional: {additionalInfo}");
                }
                catch
                {
                    // Last resort logging
                    _logger.LogError("Complete failure to log {Action} action for {EntityType}", action, entityType);
                }
            }
        }

        private string GetEntityId(object entity)
        {
            try
            {
                var idProperty = entity.GetType().GetProperty("Id");
                if (idProperty != null)
                {
                    var id = idProperty.GetValue(entity);
                    return id?.ToString() ?? "unknown";
                }
                
                // Check for other possible ID properties
                var requestNumberProperty = entity.GetType().GetProperty("RequestNumber");
                if (requestNumberProperty != null)
                {
                    var requestNumber = requestNumberProperty.GetValue(entity);
                    return requestNumber?.ToString() ?? "unknown";
                }
                
                return "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private string? GetEntityName(object entity)
        {
            try
            {
                // Try common name properties
                var nameProperties = new[] { "Name", "FullName", "Title", "Subject", "Description", "RequestNumber", "Username" };
                
                foreach (var propName in nameProperties)
                {
                    var property = entity.GetType().GetProperty(propName);
                    if (property != null)
                    {
                        var value = property.GetValue(entity);
                        if (value != null && !string.IsNullOrEmpty(value.ToString()))
                        {
                            return value.ToString();
                        }
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string SerializeEntity(object entity)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Create a simplified version of the entity without navigation properties
                var entityType = entity.GetType();
                var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && IsSimpleType(p.PropertyType))
                    .ToDictionary(p => p.Name, p => p.GetValue(entity));

                return JsonSerializer.Serialize(properties, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error serializing entity {EntityType}", entity.GetType().Name);
                return $"Error serializing {entity.GetType().Name}";
            }
        }

        private bool IsSimpleType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type)!;
            }

            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(decimal) ||
                   type == typeof(Guid) ||
                   type.IsEnum;
        }

        private async Task<List<AuditLog>> GetLoginLogsAsAuditEntriesAsync()
        {
            try
            {
                var loginLogs = await _context.LoginLogs
                    .Include(l => l.User)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();

                return loginLogs.Select(ConvertLoginLogToAuditLog).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving login logs as audit entries");
                return new List<AuditLog>();
            }
        }

        private async Task<List<AuditLog>> GetFilteredLoginLogsAsAuditEntriesAsync(string? action = null, int? userId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.LoginLogs
                    .Include(l => l.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(l => l.Action == action);

                if (userId.HasValue)
                    query = query.Where(l => l.UserId == userId.Value);

                if (startDate.HasValue)
                    query = query.Where(l => l.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(l => l.Timestamp <= endDate.Value);

                var loginLogs = await query
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();

                return loginLogs.Select(ConvertLoginLogToAuditLog).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered login logs as audit entries");
                return new List<AuditLog>();
            }
        }

        private AuditLog ConvertLoginLogToAuditLog(LoginLog loginLog)
        {
            var statusInfo = loginLog.IsSuccessful ? "SUCCESS" : "FAILED";
            var additionalInfo = "";

            if (!loginLog.IsSuccessful && !string.IsNullOrEmpty(loginLog.FailureReason))
            {
                additionalInfo = $"Reason: {loginLog.FailureReason}";
            }

            if (!string.IsNullOrEmpty(loginLog.IpAddress))
            {
                additionalInfo += string.IsNullOrEmpty(additionalInfo) ? $"IP: {loginLog.IpAddress}" : $", IP: {loginLog.IpAddress}";
            }

            if (loginLog.SessionDurationMinutes.HasValue)
            {
                additionalInfo += string.IsNullOrEmpty(additionalInfo) ? $"Duration: {loginLog.SessionDurationMinutes}min" : $", Duration: {loginLog.SessionDurationMinutes}min";
            }

            return new AuditLog
            {
                Id = loginLog.Id + 1000000, // Offset to avoid ID conflicts
                EntityType = "Security",
                Action = loginLog.Action,
                EntityId = loginLog.Id.ToString(),
                EntityName = loginLog.Username,
                OldValues = null,
                NewValues = statusInfo,
                AdditionalInfo = additionalInfo,
                Timestamp = loginLog.Timestamp,
                UserId = loginLog.UserId,
                UserName = loginLog.User?.FullName ?? loginLog.Username,
                User = loginLog.User
            };
        }
    }
} 