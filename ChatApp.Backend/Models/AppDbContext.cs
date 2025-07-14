using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChatApp.Backend.Models
{
    /// <summary>
    /// Main database context for the chat application
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<GroupUser> GroupUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).HasDefaultValue("Offline");
            });

            // Configure Group entity
            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.InviteCode).HasMaxLength(50);
                entity.HasIndex(e => e.InviteCode).IsUnique();
                
                // Group creator relationship
                entity.HasOne<User>()
                    .WithMany(u => u.CreatedGroups)
                    .HasForeignKey(g => g.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Message entity
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
                
                // Convert List<string> to JSON for attachment URLs
                entity.Property(e => e.AttachmentUrls)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

                // User relationship
                entity.HasOne(m => m.User)
                    .WithMany(u => u.Messages)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Group relationship
                entity.HasOne(m => m.Group)
                    .WithMany(g => g.Messages)
                    .HasForeignKey(m => m.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Reply relationship (self-referencing)
                entity.HasOne(m => m.ReplyToMessage)
                    .WithMany(m => m.Replies)
                    .HasForeignKey(m => m.ReplyToMessageId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes for performance
                entity.HasIndex(e => e.GroupId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsDeleted);
            });

            // Configure GroupUser entity (many-to-many with additional properties)
            modelBuilder.Entity<GroupUser>(entity =>
            {
                entity.HasKey(gu => new { gu.UserId, gu.GroupId });
                entity.Property(e => e.Role).HasDefaultValue("Member").HasMaxLength(20);

                entity.HasOne(gu => gu.User)
                    .WithMany(u => u.GroupUsers)
                    .HasForeignKey(gu => gu.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(gu => gu.Group)
                    .WithMany(g => g.GroupUsers)
                    .HasForeignKey(gu => gu.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.IsApproved);
                entity.HasIndex(e => e.IsBanned);
                entity.HasIndex(e => e.JoinedAt);
            });

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.DeviceInfo).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(45);

                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes for performance
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.UserId);
            });
        }
    }
} 