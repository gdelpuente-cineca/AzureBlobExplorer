using AzureBlobExplorer.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureBlobExplorer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AccessPolicy> AccessPolicies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserProfile configuration
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DisplayName).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Role).HasDefaultValue(UserRole.Guest);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => new { e.UserId, e.Timestamp });
            });

            // AccessPolicy configuration
            modelBuilder.Entity<AccessPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.ContainerName).IsRequired();
                entity.Property(e => e.Permission).HasDefaultValue(BlobPermission.Read);
                entity.HasIndex(e => new { e.UserId, e.ContainerName });
            });
        }
    }
}
