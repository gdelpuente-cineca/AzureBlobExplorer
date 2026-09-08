using AzureBlobExplorer.Data;
using AzureBlobExplorer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AzureBlobExplorer.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogActionAsync(string userId, string action, string? container, string? blob,
            long? fileSize = null, bool isSuccessful = true, string? errorMessage = null)
        {
            try
            {
                var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    ContainerName = container,
                    BlobName = blob,
                    FileSizeBytes = fileSize,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    IsSuccessful = isSuccessful,
                    ErrorMessage = errorMessage
                };

                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Audit logged: User {userId} performed {action} on {container}/{blob}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit trail");
                // Don't throw - audit logging should not block operations
            }
        }

        public async Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await _dbContext.AuditLogs
                    .Where(l => l.UserId == userId && l.Timestamp >= cutoffDate)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving audit logs for user {userId}");
                return new List<AuditLog>();
            }
        }

        public async Task<List<AuditLog>> GetContainerAuditLogsAsync(string container, int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await _dbContext.AuditLogs
                    .Where(l => l.ContainerName == container && l.Timestamp >= cutoffDate)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving audit logs for container {container}");
                return new List<AuditLog>();
            }
        }

        public async Task<List<AuditLog>> GetAllAuditLogsAsync(int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await _dbContext.AuditLogs
                    .Where(l => l.Timestamp >= cutoffDate)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all audit logs");
                return new List<AuditLog>();
            }
        }
    }
}
