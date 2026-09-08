namespace AzureBlobExplorer.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; } // Download, Upload, Delete, Browse
        public string? ContainerName { get; set; }
        public string? BlobName { get; set; }
        public string? Path { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public bool IsSuccessful { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }
}
