using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.Production.Domain;

namespace Nexo.Production.Infrastructure.Configurations;

public sealed class ProductionRunConfiguration : IEntityTypeConfiguration<ProductionRun>
{
    public void Configure(EntityTypeBuilder<ProductionRun> builder)
    {
        builder.ToTable("production_runs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.WorkOrderId).IsRequired();
        builder.Property(x => x.MachineId).IsRequired();
        builder.Property(x => x.ShiftId).IsRequired();
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.ClosedAt);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(x => x.WorkOrderId);

        builder.HasMany(x => x.Records)
            .WithOne()
            .HasForeignKey(r => r.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        // The Records collection is exposed read-only over the private _records backing field.
        builder.Navigation(x => x.Records).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
