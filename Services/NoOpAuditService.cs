using AzureBlobExplorer.Models;

namespace AzureBlobExplorer.Services
{
    /// <summary>
    /// No-op implementation of IAuditService for when audit logging is disabled
    /// </summary>
    public class NoOpAuditService : IAuditService
    {
        public Task LogActionAsync(string userId, string action, string? container, string? blob,
            long? fileSize = null, bool isSuccessful = true, string? errorMessage = null)
        {
            // Do nothing
            return Task.CompletedTask;
        }

        public Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int days = 30)
        {
            return Task.FromResult(new List<AuditLog>());
        }

        public Task<List<AuditLog>> GetContainerAuditLogsAsync(string container, int days = 30)
        {
            return Task.FromResult(new List<AuditLog>());
        }

        public Task<List<AuditLog>> GetAllAuditLogsAsync(int days = 30)
        {
            return Task.FromResult(new List<AuditLog>());
        }
    }
}
