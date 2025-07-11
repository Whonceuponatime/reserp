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
        
        // Security Review Statement form
        public DbSet<SecurityReviewStatement> SecurityReviewStatements { get; set; }

        // Audit Logs
        public DbSet<AuditLog> AuditLogs { get; set; }
        
        // Document Management
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        
        // Login Logs
        public DbSet<LoginLog> LoginLogs { get; set; }

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

            // Configure SecurityReviewStatement relationships
            modelBuilder.Entity<SecurityReviewStatement>(entity =>
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

            // Configure AuditLog relationships
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
                entity.Property(e => e.EntityId).IsRequired();
                entity.Property(e => e.EntityName).HasMaxLength(200);
                entity.Property(e => e.TableName).HasMaxLength(50);
                entity.Property(e => e.UserName).HasMaxLength(200);
                entity.Property(e => e.IpAddress).HasMaxLength(100);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.AdditionalInfo).HasMaxLength(500);
                entity.Property(e => e.Timestamp).IsRequired();
                
                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
                      
                entity.HasIndex(e => e.EntityType);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.UserId);
            });

            // Configure Document relationships
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.FileName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.FileExtension).HasMaxLength(10).IsRequired();
                entity.Property(e => e.FileHash).HasMaxLength(32).IsRequired();
                entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Comments).HasMaxLength(1000);

                entity.HasOne(d => d.Category)
                      .WithMany(p => p.Documents)
                      .HasForeignKey(d => d.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(d => d.Ship)
                      .WithMany(s => s.Documents)
                      .HasForeignKey(d => d.ShipId)
                      .OnDelete(DeleteBehavior.SetNull);
                      
                entity.HasOne(d => d.UploadedBy)
                      .WithMany()
                      .HasForeignKey(d => d.UploadedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(d => d.ApprovedBy)
                      .WithMany()
                      .HasForeignKey(d => d.ApprovedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
                      
                entity.HasOne(d => d.PreviousVersion)
                      .WithMany(p => p.NewerVersions)
                      .HasForeignKey(d => d.PreviousVersionId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.FileHash);
                entity.HasIndex(e => e.UploadedAt);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.ShipId);
            });

            // Configure DocumentCategory
            modelBuilder.Entity<DocumentCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.AllowedFileTypes).HasMaxLength(500);

                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.DisplayOrder);
            });

            // Configure DocumentVersion relationships
            modelBuilder.Entity<DocumentVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
                entity.Property(e => e.FileHash).HasMaxLength(32).IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(200).IsRequired();
                entity.Property(e => e.ChangeDescription).HasMaxLength(1000);

                entity.HasOne(d => d.Document)
                      .WithMany(p => p.Versions)
                      .HasForeignKey(d => d.DocumentId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(d => d.UploadedBy)
                      .WithMany()
                      .HasForeignKey(d => d.UploadedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.VersionNumber);
                entity.HasIndex(e => e.UploadedAt);
            });

            // Configure LoginLog
            modelBuilder.Entity<LoginLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
                entity.Property(e => e.FailureReason).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(100);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Device).HasMaxLength(200);
                entity.Property(e => e.Location).HasMaxLength(100);
                entity.Property(e => e.AdditionalInfo).HasMaxLength(500);

                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Username);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.IsSuccessful);
                entity.HasIndex(e => e.IsSecurityEvent);
            });

            // Seed data
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Administrator", Description = "Full system access - can manage users, approve/reject forms, edit all data" },
                new Role { Id = 2, Name = "Engineer", Description = "Normal user - can submit forms and view data (read-only)" }
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
                new ChangeType { Id = 3, Name = "System Plan", Description = "System planning and design change" },
                new ChangeType { Id = 4, Name = "Security Review Statement", Description = "Security review statement" }
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

            // Seed Document Categories
            modelBuilder.Entity<DocumentCategory>().HasData(
                // Approved Supplier Documentation
                new DocumentCategory 
                { 
                    Id = 1, 
                    Name = "Zones and Conduit Diagram", 
                    Category = "Approved Supplier Documentation",
                    Description = "Detailed diagrams showing vessel zones and conduit layouts for cyber security planning",
                    IsRequired = true,
                    AllowedFileTypes = "pdf,dwg,dxf,png,jpg,jpeg",
                    MaxFileSizeBytes = 100 * 1024 * 1024, // 100MB
                    DisplayOrder = 1,
                    IsActive = true
                },
                new DocumentCategory 
                { 
                    Id = 2, 
                    Name = "Cyber Security Design Description", 
                    Category = "Approved Supplier Documentation",
                    Description = "Comprehensive document describing the cyber security design and architecture",
                    IsRequired = true,
                    AllowedFileTypes = "pdf,doc,docx",
                    MaxFileSizeBytes = 50 * 1024 * 1024, // 50MB
                    DisplayOrder = 2,
                    IsActive = true
                },
                new DocumentCategory 
                { 
                    Id = 3, 
                    Name = "Vessel Asset Inventory", 
                    Category = "Approved Supplier Documentation",
                    Description = "Complete inventory of all vessel assets including IT and OT systems",
                    IsRequired = true,
                    AllowedFileTypes = "pdf,doc,docx,xlsx,csv",
                    MaxFileSizeBytes = 25 * 1024 * 1024, // 25MB
                    DisplayOrder = 3,
                    IsActive = true
                },
                new DocumentCategory 
                { 
                    Id = 4, 
                    Name = "Risk Assessment for Exclusion of CBSs", 
                    Category = "Approved Supplier Documentation",
                    Description = "Risk assessment documentation for the exclusion of Critical Business Systems",
                    IsRequired = true,
                    AllowedFileTypes = "pdf,doc,docx",
                    MaxFileSizeBytes = 50 * 1024 * 1024, // 50MB
                    DisplayOrder = 4,
                    IsActive = true
                },
                new DocumentCategory 
                { 
                    Id = 5, 
                    Name = "Description of Compensating Countermeasures", 
                    Category = "Approved Supplier Documentation",
                    Description = "Documentation describing compensating countermeasures for identified security gaps",
                    IsRequired = true,
                    AllowedFileTypes = "pdf,doc,docx",
                    MaxFileSizeBytes = 50 * 1024 * 1024, // 50MB
                    DisplayOrder = 5,
                    IsActive = true
                },
                new DocumentCategory 
                { 
                    Id = 6, 
                    Name = "Ship Cyber Resilience Test Procedure", 
                    Category = "Approved Supplier Documentation",
                    Description = "Detailed procedures for testing ship cyber resilience and security measures",
                    IsRequired = true,
                    AllowedFileTypes = "pdf,doc,docx",
                    MaxFileSizeBytes = 50 * 1024 * 1024, // 50MB
                    DisplayOrder = 6,
                    IsActive = true
                }
            );
        }
    }
}
