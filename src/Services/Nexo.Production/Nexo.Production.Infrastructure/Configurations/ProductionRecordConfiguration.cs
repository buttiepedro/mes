using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.Production.Domain;

namespace Nexo.Production.Infrastructure.Configurations;

public sealed class ProductionRecordConfiguration : IEntityTypeConfiguration<ProductionRecord>
{
    public void Configure(EntityTypeBuilder<ProductionRecord> builder)
    {
        builder.ToTable("production_records");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.RunId).IsRequired();

        builder.Property(x => x.GoodQty)
            .HasConversion(q => q.Value, v => Quantity.Of(v))
            .HasColumnName("good_qty")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.ScrapQty)
            .HasConversion(q => q.Value, v => Quantity.Of(v))
            .HasColumnName("scrap_qty")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.OperatorId).IsRequired();
        builder.Property(x => x.RecordedAt).IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(x => x.RunId);
    }
}
