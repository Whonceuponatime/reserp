using Microsoft.EntityFrameworkCore;
using MaritimeERP.Core.Entities;

namespace MaritimeERP.Data
{
    public class MaritimeERPContext : DbContext
    {
        public MaritimeERPContext(DbContextOptions<MaritimeERPContext> options) : base(options)
        {
        }

        public DbSet<Ship> Ships { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<ShipSystem> Systems { get; set; }
        public DbSet<SystemCategory> SystemCategories { get; set; }
        public DbSet<SecurityZone> SecurityZones { get; set; }
        public DbSet<Component> Components { get; set; }
        public DbSet<Software> Software { get; set; }
        
        // Change Request related entities
        public DbSet<ChangeRequest> ChangeRequests { get; set; }
        public DbSet<ChangeType> ChangeTypes { get; set; }
        public DbSet<ChangeStatus> ChangeStatuses { get; set; }
        public DbSet<HardwareChangeDetail> HardwareChangeDetails { get; set; }
        public DbSet<SoftwareChangeDetail> SoftwareChangeDetails { get; set; }
        public DbSet<SystemPlanDetail> SystemPlanDetails { get; set; }
        public DbSet<Approval> Approvals { get; set; }
        public DbSet<SecurityReviewItem> SecurityReviewItems { get; set; }
        
        // Hardware Change Request form
        public DbSet<HardwareChangeRequest> HardwareChangeRequests { get; set; }
        
        // Software Change Request form
        public DbSet<SoftwareChangeRequest> SoftwareChangeRequests { get; set; }
        
        // System Change Plan form
        public DbSet<SystemChangePlan> SystemChangePlans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ship>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ShipName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.ImoNumber).HasMaxLength(7).IsRequired();
                // Remove unique constraint to allow soft-deleted records to reuse IMO numbers
                // Uniqueness is enforced in the application layer
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
                entity.HasOne(d => d.Role).WithMany(p => p.Users).HasForeignKey(d => d.RoleId);
            });

            modelBuilder.Entity<ShipSystem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Manufacturer).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Model).HasMaxLength(100).IsRequired();
                entity.Property(e => e.SerialNumber).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.HasRemoteConnection).HasDefaultValue(false);
                entity.HasOne(d => d.Ship).WithMany(p => p.Systems).HasForeignKey(d => d.ShipId);
                entity.HasOne(d => d.Category).WithMany(p => p.Systems).HasForeignKey(d => d.CategoryId);
                entity.HasOne(d => d.SecurityZone).WithMany(p => p.Systems).HasForeignKey(d => d.SecurityZoneId);
                entity.HasIndex(e => e.SerialNumber).IsUnique();
            });

            modelBuilder.Entity<SystemCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            modelBuilder.Entity<SecurityZone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasMany(e => e.Systems).WithOne(e => e.SecurityZone).HasForeignKey(e => e.SecurityZoneId);
            });

            modelBuilder.Entity<Component>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SystemName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.ComponentType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Manufacturer).HasMaxLength(200);
                entity.Property(e => e.Model).HasMaxLength(200);
                entity.Property(e => e.InstalledLocation).HasMaxLength(200).IsRequired();
                entity.Property(e => e.OsName).HasMaxLength(200);
                entity.Property(e => e.OsVersion).HasMaxLength(50);
                entity.Property(e => e.SupportedProtocols).HasMaxLength(200);
                entity.Property(e => e.NetworkSegment).HasMaxLength(100);
                entity.Property(e => e.ConnectedCbs).HasMaxLength(500);
                entity.Property(e => e.ConnectionPurpose).HasMaxLength(500);
                entity.HasOne(d => d.System).WithMany(p => p.Components).HasForeignKey(d => d.SystemId);
            });

            modelBuilder.Entity<Software>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Manufacturer).HasMaxLength(200);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.SoftwareType).HasMaxLength(100);
                entity.Property(e => e.Version).HasMaxLength(50);
                entity.Property(e => e.FunctionPurpose).HasMaxLength(500);
                entity.Property(e => e.InstalledHardwareComponent).HasMaxLength(200);
                entity.Property(e => e.LicenseType).HasMaxLength(100);
                entity.Property(e => e.LicenseKey).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.InstallationDate);
                entity.Property(e => e.ExpiryDate);
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.UpdatedAt);

                entity.HasOne(e => e.InstalledComponent)
                    .WithMany()
                    .HasForeignKey(e => e.InstalledComponentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure ChangeRequest relationships
            modelBuilder.Entity<ChangeRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequestNo).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Purpose).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.HasOne(d => d.Ship).WithMany(p => p.ChangeRequests).HasForeignKey(d => d.ShipId);
                entity.HasOne(d => d.RequestType).WithMany().HasForeignKey(d => d.RequestTypeId);
                entity.HasOne(d => d.Status).WithMany().HasForeignKey(d => d.StatusId);
                entity.HasOne(d => d.RequestedBy).WithMany().HasForeignKey(d => d.RequestedById);
                
                // Configure one-to-one relationships with detail entities
                entity.HasOne(e => e.HardwareChangeDetail)
                      .WithOne(e => e.ChangeRequest)
                      .HasForeignKey<HardwareChangeDetail>(e => e.ChangeRequestId);
                
                entity.HasOne(e => e.SoftwareChangeDetail)
                      .WithOne(e => e.ChangeRequest)
                      .HasForeignKey<SoftwareChangeDetail>(e => e.ChangeRequestId);
                
                entity.HasOne(e => e.SystemPlanDetail)
                      .WithOne(e => e.ChangeRequest)
                      .HasForeignKey<SystemPlanDetail>(e => e.ChangeRequestId);
            });

            modelBuilder.Entity<HardwareChangeDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PreHardwareModel).HasMaxLength(200);
                entity.Property(e => e.PostHardwareModel).HasMaxLength(200);
                entity.Property(e => e.PreOperatingSystem).HasMaxLength(200);
                entity.Property(e => e.PostOperatingSystem).HasMaxLength(200);
                entity.Property(e => e.WorkDetails).HasMaxLength(2000);
            });

            modelBuilder.Entity<SoftwareChangeDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PreSoftwareName).HasMaxLength(200);
                entity.Property(e => e.PostSoftwareName).HasMaxLength(200);
                entity.Property(e => e.PreSoftwareVersion).HasMaxLength(50);
                entity.Property(e => e.PostSoftwareVersion).HasMaxLength(50);
                entity.Property(e => e.WorkDetails).HasMaxLength(2000);
            });

            modelBuilder.Entity<SystemPlanDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PlanDetails).HasMaxLength(5000);
            });

            // Configure HardwareChangeRequest relationships
            modelBuilder.Entity<HardwareChangeRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequestNumber).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.RequestNumber).IsUnique();
                
                entity.HasOne(d => d.RequesterUser)
                      .WithMany()
                      .HasForeignKey(d => d.RequesterUserId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(d => d.PreparedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.PreparedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(d => d.ReviewedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.ReviewedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(d => d.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.ApprovedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure SoftwareChangeRequest relationships
            modelBuilder.Entity<SoftwareChangeRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequestNumber).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.RequestNumber).IsUnique();
                
                entity.HasOne(d => d.RequesterUser)
                      .WithMany()
                      .HasForeignKey(d => d.RequesterUserId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(d => d.PreparedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.PreparedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(d => d.ReviewedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.ReviewedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(d => d.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.ApprovedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure SystemChangePlan relationships
            modelBuilder.Entity<SystemChangePlan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequestNumber).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.RequestNumber).IsUnique();
                
                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
                      
                entity.HasOne(d => d.Ship)
                      .WithMany()
                      .HasForeignKey(d => d.ShipId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed data
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Administrator", Description = "Full system access" },
                new Role { Id = 2, Name = "Manager", Description = "Management access" },
                new Role { Id = 3, Name = "Engineer", Description = "Engineering access" },
                new Role { Id = 4, Name = "Reviewer", Description = "Review access" }
            );

            modelBuilder.Entity<SystemCategory>().HasData(
                new SystemCategory { Id = 1, Name = "Navigation", Description = "Navigation and positioning systems" },
                new SystemCategory { Id = 2, Name = "Engine", Description = "Engine and propulsion systems" },
                new SystemCategory { Id = 3, Name = "Safety", Description = "Safety and emergency systems" },
                new SystemCategory { Id = 4, Name = "Communication", Description = "Communication systems" },
                new SystemCategory { Id = 5, Name = "Cargo", Description = "Cargo handling systems" }
            );

            modelBuilder.Entity<SecurityZone>().HasData(
                new SecurityZone { Id = 1, Name = "Bridge", Description = "Navigation bridge area" },
                new SecurityZone { Id = 2, Name = "Engine Room", Description = "Engine room area" },
                new SecurityZone { Id = 3, Name = "Cargo Hold", Description = "Cargo storage area" },
                new SecurityZone { Id = 4, Name = "Accommodation", Description = "Living quarters" },
                new SecurityZone { Id = 5, Name = "Deck", Description = "Main deck area" }
            );
            
            // Seed Change Types
            modelBuilder.Entity<ChangeType>().HasData(
                new ChangeType { Id = 1, Name = "Hardware Change", Description = "Hardware modification or replacement" },
                new ChangeType { Id = 2, Name = "Software Change", Description = "Software update or configuration change" },
                new ChangeType { Id = 3, Name = "System Plan", Description = "System planning and design change" }
            );

            // Seed Change Statuses
            modelBuilder.Entity<ChangeStatus>().HasData(
                new ChangeStatus { Id = 1, Name = "Draft", Description = "Change request being prepared" },
                new ChangeStatus { Id = 2, Name = "Submitted", Description = "Change request submitted for review" },
                new ChangeStatus { Id = 3, Name = "Under Review", Description = "Change request under review" },
                new ChangeStatus { Id = 4, Name = "Approved", Description = "Change request approved" },
                new ChangeStatus { Id = 5, Name = "Rejected", Description = "Change request rejected" },
                new ChangeStatus { Id = 6, Name = "Completed", Description = "Change request completed" }
            );
        }
    }
}
