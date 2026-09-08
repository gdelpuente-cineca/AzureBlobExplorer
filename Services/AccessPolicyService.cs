using AzureBlobExplorer.Data;
using AzureBlobExplorer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AzureBlobExplorer.Services
{
    public class AccessPolicyService : IAccessPolicyService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AccessPolicyService> _logger;

        public AccessPolicyService(
            ApplicationDbContext dbContext,
            ILogger<AccessPolicyService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<string>> GetAccessibleContainersAsync(string userId)
        {
            try
            {
                var policies = await _dbContext.AccessPolicies
                    .Where(p => p.UserId == userId && p.IsActive && 
                           (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow))
                    .Select(p => p.ContainerName)
                    .Distinct()
                    .ToListAsync();

                return policies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting accessible containers for user {userId}");
                return new List<string>();
            }
        }

        public async Task<bool> HasAccessAsync(string userId, string container, string? path, BlobPermission requiredPermission)
        {
            try
            {
                var policy = await _dbContext.AccessPolicies
                    .Where(p => p.UserId == userId && 
                           p.ContainerName == container &&
                           p.IsActive && 
                           (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow))
                    .FirstOrDefaultAsync();

                if (policy == null)
                {
                    _logger.LogWarning($"No policy found for user {userId} on container {container}");
                    return false;
                }

                // Check if path is within the allowed path
                if (!string.IsNullOrEmpty(policy.Path) && !path?.StartsWith(policy.Path, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogWarning($"User {userId} attempted access outside allowed path");
                    return false;
                }

                // Check permission
                bool hasPermission = policy.Permission == BlobPermission.All ||
                                    (requiredPermission == BlobPermission.Read && 
                                     (policy.Permission == BlobPermission.Read || policy.Permission == BlobPermission.Write)) ||
                                    policy.Permission == requiredPermission;

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking access for user {userId} on container {container}");
                return false;
            }
        }

        public async Task<List<AccessPolicy>> GetUserPoliciesAsync(string userId)
        {
            try
            {
                return await _dbContext.AccessPolicies
                    .Where(p => p.UserId == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting policies for user {userId}");
                return new List<AccessPolicy>();
            }
        }

        public async Task CreatePolicyAsync(AccessPolicy policy)
        {
            try
            {
                policy.CreatedAt = DateTime.UtcNow;
                _dbContext.AccessPolicies.Add(policy);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Policy created for user {policy.UserId} on container {policy.ContainerName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating access policy");
                throw;
            }
        }

        public async Task UpdatePolicyAsync(AccessPolicy policy)
        {
            try
            {
                _dbContext.AccessPolicies.Update(policy);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Policy updated for user {policy.UserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating access policy");
                throw;
            }
        }

        public async Task DeletePolicyAsync(int policyId)
        {
            try
            {
                var policy = await _dbContext.AccessPolicies.FindAsync(policyId);
                if (policy != null)
                {
                    _dbContext.AccessPolicies.Remove(policy);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"Policy {policyId} deleted");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting policy {policyId}");
                throw;
            }
        }
    }
}
