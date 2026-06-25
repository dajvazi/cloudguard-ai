using CloudGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Data;

public class CloudGuardDbContext(DbContextOptions<CloudGuardDbContext> options) : DbContext(options)
{
    public DbSet<CloudService> CloudServices => Set<CloudService>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<Anomaly> Anomalies => Set<Anomaly>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<RecoveryAction> RecoveryActions => Set<RecoveryAction>();
    public DbSet<TerraformUpload> TerraformUploads => Set<TerraformUpload>();
    public DbSet<Resource> Resources => Set<Resource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CloudService>(entity =>
        {
            entity.ToTable("cloud_services");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TerraformUploadId).HasColumnName("terraform_upload_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("Healthy");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.SourceKind).HasColumnName("source_kind").HasMaxLength(20).HasDefaultValue("resource");
            entity.Property(e => e.RawResourceType).HasColumnName("raw_resource_type").HasMaxLength(100);
            entity.Property(e => e.SourceFile).HasColumnName("source_file").HasMaxLength(255);
            entity.Property(e => e.ModuleSource).HasColumnName("module_source").HasMaxLength(255);
            entity.Property(e => e.ParentModule).HasColumnName("parent_module").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.TerraformUpload)
                .WithMany(u => u.CloudServices)
                .HasForeignKey(e => e.TerraformUploadId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.ToTable("metrics");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CloudServiceId).HasColumnName("cloud_service_id");
            entity.Property(e => e.MetricName).HasColumnName("metric_name").HasMaxLength(100);
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(30);
            entity.Property(e => e.CpuUsage).HasColumnName("cpu_usage").HasPrecision(10, 2);
            entity.Property(e => e.MemoryUsage).HasColumnName("memory_usage").HasPrecision(10, 2);
            entity.Property(e => e.NetworkIn).HasColumnName("network_in").HasPrecision(15, 2);
            entity.Property(e => e.NetworkOut).HasColumnName("network_out").HasPrecision(15, 2);
            entity.Property(e => e.DiskReadBytes).HasColumnName("disk_read_bytes").HasPrecision(15, 2);
            entity.Property(e => e.DiskWriteBytes).HasColumnName("disk_write_bytes").HasPrecision(15, 2);
            entity.Property(e => e.LatencyMs).HasColumnName("latency_ms").HasPrecision(10, 2);
            entity.Property(e => e.ErrorRate).HasColumnName("error_rate").HasPrecision(5, 2);
            entity.Property(e => e.Value).HasColumnName("value").HasPrecision(15, 2);
            entity.Property(e => e.Maximum).HasColumnName("maximum").HasPrecision(15, 2);
            entity.Property(e => e.Minimum).HasColumnName("minimum").HasPrecision(15, 2);
            entity.Property(e => e.RecordedAt).HasColumnName("recorded_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.CloudService)
                .WithMany(s => s.Metrics)
                .HasForeignKey(e => e.CloudServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Anomaly>(entity =>
        {
            entity.ToTable("anomalies");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CloudServiceId).HasColumnName("cloud_service_id");
            entity.Property(e => e.AnomalyType).HasColumnName("anomaly_type").HasMaxLength(100);
            entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(30);
            entity.Property(e => e.AiConfidence).HasColumnName("ai_confidence").HasPrecision(5, 2);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DetectedAt).HasColumnName("detected_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.CloudService)
                .WithMany(s => s.Anomalies)
                .HasForeignKey(e => e.CloudServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.ToTable("incidents");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CloudServiceId).HasColumnName("cloud_service_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(30);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("Open");
            entity.Property(e => e.RootCause).HasColumnName("root_cause");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");

            entity.HasOne(e => e.CloudService)
                .WithMany(s => s.Incidents)
                .HasForeignKey(e => e.CloudServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecoveryAction>(entity =>
        {
            entity.ToTable("recovery_actions");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IncidentId).HasColumnName("incident_id");
            entity.Property(e => e.ActionType).HasColumnName("action_type").HasMaxLength(100);
            entity.Property(e => e.ActionStatus).HasColumnName("action_status").HasMaxLength(30).HasDefaultValue("Pending");
            entity.Property(e => e.Description).HasColumnName("details");
            entity.Property(e => e.ExecutedAt).HasColumnName("executed_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Incident)
                .WithMany(i => i.RecoveryActions)
                .HasForeignKey(e => e.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TerraformUpload>(entity =>
        {
            entity.ToTable("terraform_uploads");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.UploadStatus).HasColumnName("upload_status").HasMaxLength(50).HasDefaultValue("Uploaded");
            entity.Property(e => e.ServicesDetected).HasColumnName("services_detected").HasDefaultValue(0);
            entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.ToTable("resources");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ResourceName).HasColumnName("resource_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResourceType).HasColumnName("resource_type").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(255);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("Discovered");
            entity.Property(e => e.DiscoveredAt).HasColumnName("discovered_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
