namespace AzureBlobExplorer.Models
{
    public class UserProfile
    {
        public string Id { get; set; } // Azure AD B2C Object ID
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; } = UserRole.Guest;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public enum UserRole
    {
        Admin,
        Guest
    }
}
