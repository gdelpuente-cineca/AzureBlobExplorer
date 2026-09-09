using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using AzureBlobExplorer.Models;
using Microsoft.Extensions.Logging;

namespace AzureBlobExplorer.Services
{
    public class AzureBlobService : IAzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<AzureBlobService> _logger;

        public AzureBlobService(
            IConfiguration configuration,
            ILogger<AzureBlobService> logger)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string not configured.");

            _blobServiceClient = new BlobServiceClient(connectionString);
            _logger = logger;
        }

        public async Task<List<string>> ListContainersAsync(string userId, IAccessPolicyService? accessPolicyService = null)
        {
            try
            {
                var containers = new List<string>();
                await foreach (var containerItem in _blobServiceClient.GetBlobContainersAsync())
                {
                    containers.Add(containerItem.Name);
                }

                // If access policy service is provided, filter by accessible containers
                if (accessPolicyService != null)
                {
                    var accessibleContainers = await accessPolicyService.GetAccessibleContainersAsync(userId);
                    if (accessibleContainers.Any())
                    {
                        containers = containers.Where(c => accessibleContainers.Contains(c)).ToList();
                    }
                    else if (!containers.Any())
                    {
                        _logger.LogWarning($"User {userId} has no container access");
                    }
                }

                return containers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error listing containers for user {userId}");
                throw;
            }
        }

        public async Task<List<BlobItem>> ListBlobsAsync(string container, string? prefix, string userId, IAccessPolicyService? accessPolicyService = null)
        {
            try
            {
                // Verify access if policy service is provided
                if (accessPolicyService != null)
                {
                    var hasAccess = await accessPolicyService.HasAccessAsync(userId, container, prefix, BlobPermission.Read);
                    if (!hasAccess)
                    {
                        _logger.LogWarning($"User {userId} denied access to container {container}");
                        throw new UnauthorizedAccessException($"Access denied to container {container}");
                    }
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(container);
                var result = new List<BlobItem>();

                await foreach (var item in containerClient.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/"))
                {
                    if (item.IsPrefix)
                    {
                        result.Add(new BlobItem
                        {
                            Name = Path.GetFileName(item.Prefix.TrimEnd('/')),
                            IsDirectory = true,
                            Size = 0,
                            FullPath = item.Prefix
                        });
                    }
                    else
                    {
                        result.Add(new BlobItem
                        {
                            Name = Path.GetFileName(item.Blob.Name),
                            IsDirectory = false,
                            Size = item.Blob.Properties.ContentLength ?? 0,
                            LastModified = item.Blob.Properties.LastModified?.UtcDateTime,
                            ContentType = item.Blob.Properties.ContentType,
                            FullPath = item.Blob.Name
                        });
                    }
                }

                return result.OrderByDescending(b => b.LastModified).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error listing blobs in container {container}");
                throw;
            }
        }

        public async Task<Stream> DownloadBlobAsync(string container, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(container);
                var blobClient = containerClient.GetBlobClient(blobName);
                var download = await blobClient.DownloadAsync();
                return download.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading blob {blobName} from container {container}");
                throw;
            }
        }

        public async Task UploadBlobAsync(string container, IFormFile file, string prefix = "")
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(container);
                var blobName = string.IsNullOrEmpty(prefix) ? file.FileName : $"{prefix}{file.FileName}";
                var blobClient = containerClient.GetBlobClient(blobName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading blob {file.FileName} to container {container}");
                throw;
            }
        }

        public async Task DeleteBlobAsync(string container, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(container);
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting blob {blobName} from container {container}");
                throw;
            }
        }

        public async Task<BlobProperties?> GetBlobPropertiesAsync(string container, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(container);
                var blobClient = containerClient.GetBlobClient(blobName);
                var properties = await blobClient.GetPropertiesAsync();

                return new BlobProperties
                {
                    Name = blobName,
                    Size = properties.Value.ContentLength,
                    LastModified = properties.Value.LastModified.UtcDateTime,
                    ContentType = properties.Value.ContentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting properties for blob {blobName}");
                return null;
            }
        }

        public async Task<Uri> GetBlobSasUriAsync(string container, string blobName, TimeSpan expiry)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(container);
                var blobClient = containerClient.GetBlobClient(blobName);

                // Note: This requires StorageSharedKeyCredential in the connection string
                var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTime.UtcNow.Add(expiry));
                return sasUri;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating SAS URI for blob {blobName}");
                throw;
            }
        }
    }
}
