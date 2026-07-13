using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.Production.Domain;

namespace Nexo.Production.Infrastructure.Configurations;

public sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();

        builder.Property(x => x.PlannedQty)
            .HasConversion(q => q.Value, v => Quantity.Of(v))
            .HasColumnName("planned_qty")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}
