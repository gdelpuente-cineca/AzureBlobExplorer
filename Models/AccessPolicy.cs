namespace AzureBlobExplorer.Models
{
    public class AccessPolicy
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string ContainerName { get; set; }
        public string? Path { get; set; } // Optional: restrict to folder within container
        public BlobPermission Permission { get; set; } = BlobPermission.Read;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public enum BlobPermission
    {
        Read,
        Write,
        Delete,
        All
    }
}
