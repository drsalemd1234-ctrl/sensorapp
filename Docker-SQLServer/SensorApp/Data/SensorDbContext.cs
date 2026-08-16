using Microsoft.EntityFrameworkCore;
using SensorApp.Models;

namespace SensorApp.Data;

public class SensorDbContext : DbContext
{
    public SensorDbContext(DbContextOptions<SensorDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<SensorReading> Readings => Set<SensorReading>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("Devices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(10);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.ToTable("Readings");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Device)
                .WithMany(d => d.Readings)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp });
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("Alerts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.HasOne(e => e.Device)
                .WithMany(d => d.Alerts)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.DeviceId);
        });
    }
}
