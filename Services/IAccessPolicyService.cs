using AzureBlobExplorer.Models;

namespace AzureBlobExplorer.Services
{
    public interface IAccessPolicyService
    {
        Task<List<string>> GetAccessibleContainersAsync(string userId);
        Task<bool> HasAccessAsync(string userId, string container, string? path, BlobPermission requiredPermission);
        Task<List<AccessPolicy>> GetUserPoliciesAsync(string userId);
        Task CreatePolicyAsync(AccessPolicy policy);
        Task UpdatePolicyAsync(AccessPolicy policy);
        Task DeletePolicyAsync(int policyId);
    }
}
