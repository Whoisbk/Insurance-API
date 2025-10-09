using InsuranceClaimsAPI.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Data
{
    public class InsuranceClaimsContext : DbContext
    {
        public InsuranceClaimsContext(DbContextOptions<InsuranceClaimsContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Models.Domain.ServiceProvider> ServiceProviders { get; set; }
        public DbSet<Policy> Policies { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ClaimDocument> ClaimDocuments { get; set; }
        public DbSet<QuoteDocument> QuoteDocuments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
            });

            // Configure ServiceProvider entity
            modelBuilder.Entity<Models.Domain.ServiceProvider>(entity =>
            {
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.HasOne(d => d.User)
                    .WithOne()
                    .HasForeignKey<Models.Domain.ServiceProvider>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Policy entity
            modelBuilder.Entity<Policy>(entity =>
            {
                entity.Property(e => e.PolicyType).HasConversion<int>();
                entity.Property(e => e.StartDate).HasColumnType("datetime");
                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.HasOne(d => d.Client)
                    .WithMany()
                    .HasForeignKey(d => d.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Claim entity
            modelBuilder.Entity<Claim>(entity =>
            {
                entity.HasIndex(e => e.ClaimNumber).IsUnique();
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.Priority).HasConversion<int>();
                entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

                entity.HasOne(d => d.Provider)
                    .WithMany(p => p.Claims)
                    .HasForeignKey(d => d.ProviderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Insurer)
                    .WithMany()
                    .HasForeignKey(d => d.InsurerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Quote entity
            modelBuilder.Entity<Quote>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.DateSubmitted).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.Policy)
                    .WithMany(p => p.Quotes)
                    .HasForeignKey(d => d.PolicyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Provider)
                    .WithMany(u => u.Quotes)
                    .HasForeignKey(d => d.ProviderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Invoice entity
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.Property(e => e.DateSubmitted).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.Quote)
                    .WithMany()
                    .HasForeignKey(d => d.QuoteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Message entity
            modelBuilder.Entity<Message>(entity =>
            {
                entity.Property(e => e.Type).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

                entity.HasOne(d => d.Claim)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.sender)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ClaimDocument entity
            modelBuilder.Entity<ClaimDocument>(entity =>
            {
                entity.Property(e => e.Type).HasConversion<int>();
                entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

                entity.HasOne(d => d.Claim)
                    .WithMany(p => p.ClaimDocuments)
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure QuoteDocument entity
            modelBuilder.Entity<QuoteDocument>(entity =>
            {
                entity.Property(e => e.Type).HasConversion<int>();
                entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

                entity.HasOne(d => d.Quote)


                    .WithMany(p => p.QuoteDocuments)
                    .HasForeignKey(d => d.QuoteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.Property(e => e.Action).HasConversion<int>();
                entity.Property(e => e.EntityType).HasConversion<int>();
                entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.DateSent).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Quote)
                    .WithMany()
                    .HasForeignKey(d => d.QuoteId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Cascade delete configurations
            modelBuilder.Entity<ClaimDocument>()
                .HasOne(cd => cd.UploadedBy)
                .WithMany()
                .HasForeignKey(cd => cd.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuoteDocument>()
                .HasOne(qd => qd.UploadedBy)
                .WithMany()
                .HasForeignKey(qd => qd.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
