using AzureBlobExplorer.Models;

namespace AzureBlobExplorer.Services
{
    /// <summary>
    /// No-op implementation of IAccessPolicyService for when access policies are disabled
    /// All users get access to all containers
    /// </summary>
    public class NoOpAccessPolicyService : IAccessPolicyService
    {
        private readonly IAzureBlobService _blobService;
        private readonly ILogger<NoOpAccessPolicyService> _logger;

        public NoOpAccessPolicyService(
            IAzureBlobService blobService,
            ILogger<NoOpAccessPolicyService> logger)
        {
            _blobService = blobService;
            _logger = logger;
        }

        public async Task<List<string>> GetAccessibleContainersAsync(string userId)
        {
            // When policies are disabled, all users can access all containers
            // Note: This still relies on Azure RBAC for actual storage access
            _logger.LogInformation($"Access policies disabled. User {userId} has access to all containers (via Azure RBAC).");
            return new List<string>(); // Return empty - UI will load all containers
        }

        public Task<bool> HasAccessAsync(string userId, string container, string? path, BlobPermission requiredPermission)
        {
            // When policies are disabled, all users have full access
            // Azure RBAC will enforce actual permissions
            _logger.LogInformation($"Access policies disabled. User {userId} has access to {container} (via Azure RBAC).");
            return Task.FromResult(true);
        }

        public Task<List<AccessPolicy>> GetUserPoliciesAsync(string userId)
        {
            return Task.FromResult(new List<AccessPolicy>());
        }

        public Task CreatePolicyAsync(AccessPolicy policy)
        {
            _logger.LogWarning("Attempted to create policy but access policies are disabled.");
            return Task.CompletedTask;
        }

        public Task UpdatePolicyAsync(AccessPolicy policy)
        {
            _logger.LogWarning("Attempted to update policy but access policies are disabled.");
            return Task.CompletedTask;
        }

        public Task DeletePolicyAsync(int policyId)
        {
            _logger.LogWarning("Attempted to delete policy but access policies are disabled.");
            return Task.CompletedTask;
        }
    }
}
