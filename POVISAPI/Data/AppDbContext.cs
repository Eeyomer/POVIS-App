using Microsoft.EntityFrameworkCore;
using POVISAPI.Models.Entity;

namespace POVISAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    // ----- Reference data -----
    public DbSet<Barangay> Barangays => Set<Barangay>();
    public DbSet<Ordinance> Ordinances => Set<Ordinance>();
    public DbSet<OrdinanceSection> OrdinanceSections => Set<OrdinanceSection>();
    public DbSet<OrdinancePenalty> OrdinancePenalties => Set<OrdinancePenalty>();

    // ----- Core records -----
    public DbSet<Violator> Violators => Set<Violator>();
    public DbSet<Violation> Violations => Set<Violation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<FiledCase> FiledCases => Set<FiledCase>();
    public DbSet<SeminarRecord> SeminarRecords => Set<SeminarRecord>();
    public DbSet<CommunityServiceRecord> CommunityServiceRecords => Set<CommunityServiceRecord>();
    public DbSet<CounselingRecord> CounselingRecords => Set<CounselingRecord>();

    // ----- Auth -----
    public DbSet<User> Users => Set<User>();
    public DbSet<ViolationEvidence> ViolationEvidence => Set<ViolationEvidence>();

    // ----- System -----
    public DbSet<TechnicalLog> TechnicalLogs => Set<TechnicalLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store every enum as string (your existing convention)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsEnum)
                {
                    property.SetProviderClrType(typeof(string));
                    property.SetMaxLength(30);
                }
            }
        }

        // ================= Barangay =================
        modelBuilder.Entity<Barangay>(e =>
        {
            e.Property(b => b.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(b => b.Name).IsUnique();
        });

        // ================= Ordinance =================
        modelBuilder.Entity<Ordinance>(e =>
        {
            e.Property(o => o.OrdinanceNumber).HasMaxLength(20).IsRequired();
            e.Property(o => o.Title).HasMaxLength(500).IsRequired();
            e.Property(o => o.ShortName).HasMaxLength(200).IsRequired();
            e.HasIndex(o => new { o.OrdinanceNumber, o.Series }).IsUnique();
        });

        // ================= OrdinanceSection =================
        modelBuilder.Entity<OrdinanceSection>(e =>
        {
            e.Property(s => s.SectionCode).HasMaxLength(50).IsRequired();
            e.Property(s => s.Description).HasMaxLength(500);

            e.HasOne(s => s.Ordinance)
             .WithMany(o => o.Sections)
             .HasForeignKey(s => s.OrdinanceId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(s => new { s.OrdinanceId, s.SectionCode }).IsUnique();
        });

        // ================= OrdinancePenalty =================
        modelBuilder.Entity<OrdinancePenalty>(e =>
        {
            e.Property(p => p.FineAmount).HasPrecision(10, 2);
            e.Property(p => p.ImprisonmentTerm).HasMaxLength(300);
            e.Property(p => p.PenaltyText).HasMaxLength(1000).IsRequired();

            e.HasOne(p => p.Ordinance)
             .WithMany(o => o.Penalties)
             .HasForeignKey(p => p.OrdinanceId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.OrdinanceSection)
             .WithMany(s => s.Penalties)
             .HasForeignKey(p => p.OrdinanceSectionId)
             .OnDelete(DeleteBehavior.Cascade);

            // One schedule row per ordinance/section/level/offender-type combo
            e.HasIndex(p => new { p.OrdinanceId, p.OrdinanceSectionId, p.OffenseLevel, p.OffenderType })
             .IsUnique();
        });

        // ================= Violator =================
        modelBuilder.Entity<Violator>(e =>
        {
            e.Property(v => v.LastName).HasMaxLength(100).IsRequired();
            e.Property(v => v.FirstName).HasMaxLength(100).IsRequired();
            e.Property(v => v.MiddleName).HasMaxLength(100);
            e.Property(v => v.Suffix).HasMaxLength(20);
            e.Property(v => v.Address).HasMaxLength(300);
            e.Property(v => v.City).HasMaxLength(100);
            e.Property(v => v.PresentedValidId).HasMaxLength(100);
            e.Property(v => v.ContactNumber).HasMaxLength(30);

            e.HasOne(v => v.Barangay)
             .WithMany(b => b.Residents)
             .HasForeignKey(v => v.BarangayId)
             .OnDelete(DeleteBehavior.SetNull);

            // Name search index for offense-history lookup at ticketing
            e.HasIndex(v => new { v.LastName, v.FirstName });
        });

        // ================= Violation =================
        modelBuilder.Entity<Violation>(e =>
        {
            e.Property(v => v.OvrNumber).HasMaxLength(20).IsRequired();
            e.Property(v => v.PlaceOfOccurrence).HasMaxLength(300);
            e.Property(v => v.Witness).HasMaxLength(150);
            e.Property(v => v.ApprehendingOfficer).HasMaxLength(150);
            e.Property(v => v.ApprehendingOfficerDeputationNumber).HasMaxLength(50);
            e.Property(v => v.Remarks).HasMaxLength(500);
            e.Property(v => v.Remarks2).HasMaxLength(500);

            e.HasOne(v => v.Violator)
             .WithMany(x => x.Violations)
             .HasForeignKey(v => v.ViolatorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(v => v.Ordinance)
             .WithMany(o => o.Violations)
             .HasForeignKey(v => v.OrdinanceId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(v => v.OrdinanceSection)
             .WithMany(s => s.Violations)
             .HasForeignKey(v => v.OrdinanceSectionId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(v => v.BarangayOfOccurrence)
             .WithMany(b => b.ViolationsOccurred)
             .HasForeignKey(v => v.BarangayOfOccurrenceId)
             .OnDelete(DeleteBehavior.SetNull);

            // Dual violations share one OVR ticket, so uniqueness is per ordinance
            e.HasIndex(v => new { v.OvrNumber, v.OrdinanceId }).IsUnique();

            // Report-friendly indexes (MONTHLY / SEMI / PER BRGY aggregations)
            e.HasIndex(v => v.DateOfOccurrence);
            e.HasIndex(v => v.Status);
        });

        // ================= Payment (1:1) =================
        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(10, 2);
            e.Property(p => p.ReceiptNumber).HasMaxLength(50);
            e.Property(p => p.PaidAt).HasMaxLength(100);
            e.Property(p => p.Remarks).HasMaxLength(500);

            e.HasOne(p => p.Violation)
             .WithOne(v => v.Payment)
             .HasForeignKey<Payment>(p => p.ViolationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => p.ViolationId).IsUnique();
            e.HasIndex(p => p.DateOfPayment);
        });

        // ================= FiledCase (1:1) =================
        modelBuilder.Entity<FiledCase>(e =>
        {
            e.Property(f => f.NpsDocketNumber).HasMaxLength(50);
            e.Property(f => f.ResolutionRemarks).HasMaxLength(500);
            e.Property(f => f.Remarks).HasMaxLength(500);

            e.HasOne(f => f.Violation)
             .WithOne(v => v.FiledCase)
             .HasForeignKey<FiledCase>(f => f.ViolationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(f => f.ViolationId).IsUnique();
            e.HasIndex(f => f.DateFiled);
        });

        // ================= SeminarRecord =================
        modelBuilder.Entity<SeminarRecord>(e =>
        {
            e.Property(s => s.RequiredDays).HasPrecision(4, 1); // Ord. 07 has 2.5 days
            e.Property(s => s.Remarks).HasMaxLength(500);

            e.HasOne(s => s.Violation)
             .WithMany(v => v.SeminarRecords)
             .HasForeignKey(s => s.ViolationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ================= CommunityServiceRecord =================
        modelBuilder.Entity<CommunityServiceRecord>(e =>
        {
            e.Property(c => c.RequiredHours).HasPrecision(5, 1);
            e.Property(c => c.AssignedLocation).HasMaxLength(200);
            e.Property(c => c.Remarks).HasMaxLength(500);

            e.HasOne(c => c.Violation)
             .WithMany(v => v.CommunityServiceRecords)
             .HasForeignKey(c => c.ViolationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ================= CounselingRecord =================
        modelBuilder.Entity<CounselingRecord>(e =>
        {
            e.Property(c => c.InterventionType).HasMaxLength(200);
            e.Property(c => c.ConductedBy).HasMaxLength(150);
            e.Property(c => c.Remarks).HasMaxLength(500);

            e.HasOne(c => c.Violation)
             .WithMany(v => v.CounselingRecords)
             .HasForeignKey(c => c.ViolationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ================= User =================
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Username).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.DeputationNumber).HasMaxLength(50);
            e.Property(u => u.FullName).HasMaxLength(150);
            e.Property(u => u.ContactNumber).HasMaxLength(30);
            e.Property(u => u.Email).HasMaxLength(150);
            e.Property(u => u.Address).HasMaxLength(300);
            e.Property(u => u.Photo).HasColumnType("text");
            e.HasIndex(u => u.DeputationNumber).IsUnique().HasFilter("\"DeputationNumber\" IS NOT NULL");
        });

        // ================= ViolationEvidence =================
        modelBuilder.Entity<ViolationEvidence>(e =>
        {
            e.Property(v => v.ContentType).HasMaxLength(20).IsRequired();
            e.Property(v => v.ImageData).HasColumnType("text").IsRequired();
            e.HasOne(v => v.Violation)
             .WithMany(v => v.Evidence)
             .HasForeignKey(v => v.ViolationId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(v => v.ViolationId);
        });

        // ================= AppSetting =================
        modelBuilder.Entity<AppSetting>(e =>
        {
            e.HasKey(s => s.Key);
            e.Property(s => s.Key).HasMaxLength(100);
            e.Property(s => s.Value).HasMaxLength(500).IsRequired();
        });

        // ================= TechnicalLog =================
        modelBuilder.Entity<TechnicalLog>(e =>
        {
            e.Property(t => t.Method).HasMaxLength(10).IsRequired();
            e.Property(t => t.Path).HasMaxLength(300).IsRequired();
            e.Property(t => t.Username).HasMaxLength(100);
            e.Property(t => t.IpAddress).HasMaxLength(50);
            e.HasIndex(t => t.Timestamp);
        });
    }

    // Auto-maintain BaseEntity audit timestamps
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
