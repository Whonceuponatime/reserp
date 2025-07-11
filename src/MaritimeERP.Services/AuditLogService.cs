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
                return await _context.AuditLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();
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

                if (!string.IsNullOrEmpty(entityType))
                    query = query.Where(a => a.EntityType == entityType);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(a => a.Action == action);

                if (userId.HasValue)
                    query = query.Where(a => a.UserId == userId.Value);

                if (startDate.HasValue)
                    query = query.Where(a => a.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.Timestamp <= endDate.Value);

                return await query
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();
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
                return await _context.AuditLogs
                    .Select(a => a.EntityType)
                    .Distinct()
                    .OrderBy(et => et)
                    .ToListAsync();
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
                    UserId = currentUser?.Id,
                    UserName = currentUser?.FullName ?? "System"
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit entry for {EntityType} {Action} {EntityId}", entityType, action, entityId);
                // Don't throw here to avoid breaking the main operation
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

                await LogAsync(entityType, "CREATE", entityId, entityName, null, newValues, additionalInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging create audit entry for {EntityType}", typeof(T).Name);
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

                await LogAsync(entityType, "UPDATE", entityId, entityName, oldValues, newValues, additionalInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging update audit entry for {EntityType}", typeof(T).Name);
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

                await LogAsync(entityType, "DELETE", entityId, entityName, oldValues, null, additionalInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging delete audit entry for {EntityType}", typeof(T).Name);
            }
        }

        public async Task LogActionAsync(string entityType, string action, string entityId, string? entityName = null, string? additionalInfo = null)
        {
            try
            {
                await LogAsync(entityType, action, entityId, entityName, null, null, additionalInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging action audit entry for {EntityType} {Action}", entityType, action);
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
    }
} 