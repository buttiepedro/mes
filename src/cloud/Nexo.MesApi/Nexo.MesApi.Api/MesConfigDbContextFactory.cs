using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexo.MesApi.Infrastructure;

namespace Nexo.MesApi.Api;

/// <summary>
/// Factory de diseño para <c>dotnet ef</c> (migraciones). Vive en el proyecto de arranque para que EF
/// la use en lugar de instanciar el DbContext vía el host (que exige un tenant resuelto). Conexión local
/// fija al tenant demo.
/// </summary>
public sealed class MesConfigDbContextFactory : IDesignTimeDbContextFactory<MesConfigDbContext>
{
    public MesConfigDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MesConfigDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=nexo_tenant_demo;Username=nexo;Password=nexo")
            .Options;

        return new MesConfigDbContext(options);
    }
}
