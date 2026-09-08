using AzureBlobExplorer.Models;

namespace AzureBlobExplorer.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(string userId, string action, string? container, string? blob, 
            long? fileSize = null, bool isSuccessful = true, string? errorMessage = null);
        
        Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int days = 30);
        
        Task<List<AuditLog>> GetContainerAuditLogsAsync(string container, int days = 30);
        
        Task<List<AuditLog>> GetAllAuditLogsAsync(int days = 30);
    }
}
