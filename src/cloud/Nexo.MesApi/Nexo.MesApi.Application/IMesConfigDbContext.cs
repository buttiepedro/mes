using Microsoft.EntityFrameworkCore;
using Nexo.MesApi.Domain;

namespace Nexo.MesApi.Application;

/// <summary>Puerto EF-free hacia el store de configuración (la Api/handlers dependen de esto, no del DbContext).</summary>
public interface IMesConfigDbContext
{
    DbSet<LocationNode> LocationNodes { get; }
    DbSet<Camera> Cameras { get; }
    DbSet<Zone> Zones { get; }
    DbSet<SignalDevice> SignalDevices { get; }
    DbSet<Signal> Signals { get; }
    DbSet<DetectionClass> DetectionClasses { get; }
    DbSet<VisionModel> VisionModels { get; }
    DbSet<Rule> Rules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
