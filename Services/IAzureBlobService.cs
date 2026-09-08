using AzureBlobExplorer.Models;

namespace AzureBlobExplorer.Services
{
    public interface IAzureBlobService
    {
        Task<List<string>> ListContainersAsync(string userId);
        Task<List<BlobItem>> ListBlobsAsync(string container, string? prefix, string userId);
        Task<Stream> DownloadBlobAsync(string container, string blobName);
        Task UploadBlobAsync(string container, IFormFile file, string prefix = "");
        Task DeleteBlobAsync(string container, string blobName);
        Task<BlobProperties?> GetBlobPropertiesAsync(string container, string blobName);
        Task<Uri> GetBlobSasUriAsync(string container, string blobName, TimeSpan expiry);
    }

    public class BlobProperties
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime? LastModified { get; set; }
        public string? ContentType { get; set; }
    }
}
