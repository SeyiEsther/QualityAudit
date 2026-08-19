using Microsoft.EntityFrameworkCore;
using QualityAudit.Models;

namespace QualityAudit.Data;

/// <summary>
/// EF Core context mapped onto the existing RittalQualityAudit v2 database. This app never
/// creates or migrates schema — every mapping points at a table or view that already exists.
/// EF Core 8 maps DateOnly to SQL 'date' natively, so no custom converters.
/// </summary>
public class QualityAuditContext : DbContext
{
    public QualityAuditContext(DbContextOptions<QualityAuditContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AuditItem> AuditItems => Set<AuditItem>();
    public DbSet<SeverityLevel> SeverityLevels => Set<SeverityLevel>();
    public DbSet<SeverityAssignment> SeverityAssignments => Set<SeverityAssignment>();
    public DbSet<FailureMode> FailureModes => Set<FailureMode>();
    public DbSet<NotAuditedReason> NotAuditedReasons => Set<NotAuditedReason>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<AuditUser> AuditUsers => Set<AuditUser>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Department>(e =>
        {
            e.ToTable("Departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(60);
        });

        mb.Entity<AuditItem>(e =>
        {
            e.ToTable("AuditItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(120);
            e.Property(x => x.Location).HasMaxLength(20);
            e.HasOne(x => x.Department!).WithMany().HasForeignKey(x => x.DepartmentId);
        });

        mb.Entity<SeverityLevel>(e =>
        {
            e.ToTable("SeverityLevels");
            e.HasKey(x => x.Severity);
            e.Property(x => x.Severity).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(20);
            e.Property(x => x.ColourHex).HasMaxLength(7);
            e.Property(x => x.FrequencyCode).HasMaxLength(20);
            e.Property(x => x.FrequencyLabel).HasMaxLength(60);
            e.Property(x => x.Instruction).HasMaxLength(400);
        });

        mb.Entity<SeverityAssignment>(e =>
        {
            e.ToTable("SeverityAssignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.SetBy).HasMaxLength(60);
            e.Property(x => x.SetAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.AuditItemId, x.WeekStarting }).IsUnique();
        });

        mb.Entity<FailureMode>(e =>
        {
            e.ToTable("FailureModes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(30);
            e.Property(x => x.Label).HasMaxLength(120);
        });

        mb.Entity<NotAuditedReason>(e =>
        {
            e.ToTable("NotAuditedReasons");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(30);
            e.Property(x => x.Label).HasMaxLength(120);
        });

        mb.Entity<Submission>(e =>
        {
            e.ToTable("Submissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Shift).HasMaxLength(20);
            e.Property(x => x.Auditor).HasMaxLength(60);
            e.Property(x => x.LastEditedBy).HasMaxLength(60);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            e.HasOne(x => x.Department!).WithMany().HasForeignKey(x => x.DepartmentId);
            e.HasMany(x => x.Results).WithOne(r => r.Submission!).HasForeignKey(r => r.SubmissionId);
        });

        mb.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.Id);
            e.Property(x => x.Outcome).HasColumnName("Result").HasMaxLength(12);
            e.Property(x => x.PartNo).HasMaxLength(30);
            e.Property(x => x.SerialNo).HasMaxLength(40);
            e.Property(x => x.CritDimsChecked).HasMaxLength(12);
            e.Property(x => x.QmIpVersionChecked).HasMaxLength(12);
            e.Property(x => x.Comment).HasMaxLength(500);
            e.Property(x => x.Customer).HasMaxLength(60);
            e.Property(x => x.ActionTaken).HasMaxLength(120);
            e.HasOne(x => x.AuditItem!).WithMany(a => a.Results).HasForeignKey(x => x.AuditItemId);
            e.HasOne(x => x.FailureMode!).WithMany().HasForeignKey(x => x.FailureModeId);
            e.HasOne(x => x.NotAuditedReason!).WithMany().HasForeignKey(x => x.NotAuditedReasonId);
            e.HasIndex(x => new { x.SubmissionId, x.AuditItemId }).IsUnique();
        });

        mb.Entity<AuditUser>(e =>
        {
            e.ToTable("AuditUsers");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(80);
            e.Property(x => x.Username).HasMaxLength(80);
        });

        // Views — keyless entity types.
        mb.Entity<CurrentSeverity>().HasNoKey().ToView("vw_CurrentSeverity");
        mb.Entity<WeeklyCompliance>().HasNoKey().ToView("vw_WeeklyCompliance");
        mb.Entity<VwFailure>().HasNoKey().ToView("vw_Failures");
        mb.Entity<FailuresByCustomer>().HasNoKey().ToView("vw_FailuresByCustomer");
        mb.Entity<FailureModeBreakdown>().HasNoKey().ToView("vw_FailureModeBreakdown");
        mb.Entity<WeeklySummary>().HasNoKey().ToView("vw_WeeklySummary");
    }
}
