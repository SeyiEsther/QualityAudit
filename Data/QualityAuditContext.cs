using Microsoft.EntityFrameworkCore;
using QualityAudit.Models;

namespace QualityAudit.Data;

/// <summary>
/// EF Core context mapped onto the existing RittalQualityAudit v3 database. This app never
/// creates or migrates schema — every mapping points at a table or view that already exists.
/// EF Core 8 maps DateOnly to SQL 'date' natively.
/// </summary>
public class QualityAuditContext : DbContext
{
    public QualityAuditContext(DbContextOptions<QualityAuditContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<SeverityLevel> SeverityLevels => Set<SeverityLevel>();
    public DbSet<CheckPoint> CheckPoints => Set<CheckPoint>();
    public DbSet<AuditItem> AuditItems => Set<AuditItem>();
    public DbSet<SeverityAssignment> SeverityAssignments => Set<SeverityAssignment>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ActionType> ActionTypes => Set<ActionType>();
    public DbSet<NotAuditedReason> NotAuditedReasons => Set<NotAuditedReason>();
    public DbSet<AuditUser> AuditUsers => Set<AuditUser>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<ResultCheckPoint> ResultCheckPoints => Set<ResultCheckPoint>();
    public DbSet<ResultAttachment> ResultAttachments => Set<ResultAttachment>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Department>(e => { e.ToTable("Departments"); e.HasKey(x => x.Id); });

        mb.Entity<SeverityLevel>(e =>
        {
            e.ToTable("SeverityLevels");
            e.HasKey(x => x.Severity);
            e.Property(x => x.Severity).ValueGeneratedNever();
        });

        mb.Entity<CheckPoint>(e => { e.ToTable("CheckPoints"); e.HasKey(x => x.Id); });

        mb.Entity<AuditItem>(e =>
        {
            e.ToTable("AuditItems");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Department!).WithMany().HasForeignKey(x => x.DepartmentId);
        });

        mb.Entity<SeverityAssignment>(e =>
        {
            e.ToTable("SeverityAssignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.SetAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.AuditItemId, x.WeekStarting }).IsUnique();
        });

        mb.Entity<Customer>(e => { e.ToTable("Customers"); e.HasKey(x => x.Id); });
        mb.Entity<ActionType>(e => { e.ToTable("ActionTypes"); e.HasKey(x => x.Id); });
        mb.Entity<NotAuditedReason>(e => { e.ToTable("NotAuditedReasons"); e.HasKey(x => x.Id); });
        mb.Entity<AuditUser>(e => { e.ToTable("AuditUsers"); e.HasKey(x => x.Id); });

        mb.Entity<Submission>(e =>
        {
            e.ToTable("Submissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            e.HasOne(x => x.Department!).WithMany().HasForeignKey(x => x.DepartmentId);
            e.HasMany(x => x.Results).WithOne(r => r.Submission!).HasForeignKey(r => r.SubmissionId);
        });

        mb.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.Id);
            e.Property(x => x.Outcome).HasColumnName("Result").HasMaxLength(12);
            e.HasOne(x => x.AuditItem!).WithMany(a => a.Results).HasForeignKey(x => x.AuditItemId);
            e.HasOne(x => x.NotAuditedReason!).WithMany().HasForeignKey(x => x.NotAuditedReasonId);
            e.HasOne(x => x.Customer!).WithMany().HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.ActionType!).WithMany().HasForeignKey(x => x.ActionTypeId);
            e.HasMany(x => x.CheckPoints).WithOne(c => c.Result!).HasForeignKey(c => c.ResultId);
            e.HasMany(x => x.Attachments).WithOne(a => a.Result!).HasForeignKey(a => a.ResultId);
            e.HasIndex(x => new { x.SubmissionId, x.AuditItemId }).IsUnique();
        });

        mb.Entity<ResultCheckPoint>(e =>
        {
            e.ToTable("ResultCheckPoints");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.CheckPoint!).WithMany().HasForeignKey(x => x.CheckPointId);
            e.HasIndex(x => new { x.ResultId, x.CheckPointId }).IsUnique();
        });

        mb.Entity<ResultAttachment>(e =>
        {
            e.ToTable("ResultAttachments");
            e.HasKey(x => x.Id);
            e.Property(x => x.UploadedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
        });

        // Views — keyless entity types.
        mb.Entity<CurrentSeverity>().HasNoKey().ToView("vw_CurrentSeverity");
        mb.Entity<WeeklyCompliance>().HasNoKey().ToView("vw_WeeklyCompliance");
        mb.Entity<VwFailure>().HasNoKey().ToView("vw_Failures");
        mb.Entity<FailuresByCustomer>().HasNoKey().ToView("vw_FailuresByCustomer");
        mb.Entity<CheckPointFailure>().HasNoKey().ToView("vw_CheckPointFailures");
        mb.Entity<WeeklySummary>().HasNoKey().ToView("vw_WeeklySummary");
    }
}
