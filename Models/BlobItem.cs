namespace AzureBlobExplorer.Models
{
    public class BlobItem
    {
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime? LastModified { get; set; }
        public string? ContentType { get; set; }
        public string FullPath { get; set; }
    }
}
