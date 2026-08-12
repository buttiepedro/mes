using Microsoft.EntityFrameworkCore;
using Nexo.MesApi.Application;
using Nexo.MesApi.Domain;

namespace Nexo.MesApi.Infrastructure;

/// <summary>Store de configuración del MES (schema <c>config</c>) en la tenant DB (hexa_{slug}_mes).</summary>
public sealed class MesConfigDbContext : DbContext, IMesConfigDbContext
{
    public const string Schema = "config";

    public MesConfigDbContext(DbContextOptions<MesConfigDbContext> options)
        : base(options)
    {
    }

    public DbSet<LocationNode> LocationNodes => Set<LocationNode>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<SignalDevice> SignalDevices => Set<SignalDevice>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<DetectionClass> DetectionClasses => Set<DetectionClass>();
    public DbSet<VisionModel> VisionModels => Set<VisionModel>();
    public DbSet<Rule> Rules => Set<Rule>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Schema);

        b.Entity<LocationNode>(e =>
        {
            e.ToTable("location_nodes");
            e.Property(x => x.Level).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => new { x.ParentId, x.Code }).IsUnique();
        });

        b.Entity<Camera>(e =>
        {
            e.ToTable("cameras");
            e.Property(x => x.Transport).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.AdjacentCameras).HasColumnType("jsonb");
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<Zone>(e =>
        {
            e.ToTable("zones");
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Polygon).HasColumnType("jsonb");
            e.HasIndex(x => new { x.CameraId, x.Code }).IsUnique();
        });

        b.Entity<SignalDevice>(e =>
        {
            e.ToTable("signal_devices");
            e.Property(x => x.Protocol).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Config).HasColumnType("jsonb");
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<Signal>(e =>
        {
            e.ToTable("signals");
            e.Property(x => x.ValueType).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Persistence).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.MqttTopic).HasMaxLength(400);
            e.HasIndex(x => new { x.DeviceId, x.Code }).IsUnique();
        });

        b.Entity<DetectionClass>(e =>
        {
            e.ToTable("detection_classes");
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Scope).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => new { x.Kind, x.Code }).IsUnique();
        });

        b.Entity<VisionModel>(e =>
        {
            e.ToTable("vision_models");
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(24);
            e.Property(x => x.Version).HasMaxLength(64);
            e.Property(x => x.ArtifactRef).HasMaxLength(400);
            e.Property(x => x.ProvidesClasses).HasColumnType("jsonb");
        });

        b.Entity<Rule>(e =>
        {
            e.ToTable("rules");
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Trigger).HasColumnType("jsonb");
            e.Property(x => x.Emit).HasColumnType("jsonb");
            e.HasIndex(x => x.Code).IsUnique();
        });
    }
}
