using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Infrastructure.Configurations;

/// <summary>
/// <c>execution.evidence</c> (§2.8) — a piece of evidence as a first-class business artefact. A child of
/// the <see cref="Execution"/> aggregate (the owning <c>executions → evidence</c> relationship, cascade
/// delete, is declared on <see cref="ExecutionConfiguration"/>).
/// </summary>
/// <remarks>
/// The binary never lives here: <c>file_id</c> → <c>platform.files</c> (the S3 metadata) and
/// <c>requirement_id</c> → <c>work.task_evidence_requirements</c> are logical references without a physical
/// foreign key (§1.9). Being offline-first, evidence is captured <c>pending</c> (only the reference or an
/// opaque <c>media_ref</c>) and materialized later; <c>status</c> tracks that lifecycle. <c>content_hash</c>
/// carries the integrity proof (same criterion as the event store).
/// </remarks>
public sealed class EvidenceConfiguration : IEntityTypeConfiguration<Evidence>
{
    public void Configure(EntityTypeBuilder<Evidence> builder)
    {
        builder.ToTable("evidence");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.ExecutionId).HasColumnName("execution_id").IsRequired();
        builder.Property(x => x.TaskRunId).HasColumnName("task_run_id");

        builder.Property(x => x.Kind)
            .HasColumnName("evidence_kind")
            .HasConversion(ExecutionConfigurationExtensions.EvidenceKindConverter)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(ExecutionConfigurationExtensions.EvidenceStatusConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.RequirementId).HasColumnName("requirement_id");
        builder.Property(x => x.FileId).HasColumnName("file_id");
        builder.Property(x => x.MediaRef).HasColumnName("media_ref").HasMaxLength(512);

        builder.Property(x => x.ContentHash).HasColumnName("content_hash").HasColumnType("bytea");
        builder.Property(x => x.HashAlgo).HasColumnName("hash_algo").HasMaxLength(16).IsRequired();

        builder.Property(x => x.IsMandatory).HasColumnName("is_mandatory").IsRequired();
        builder.Property(x => x.CapturedBy).HasColumnName("captured_by");
        builder.Property(x => x.Caption).HasColumnName("caption").HasMaxLength(512);
        builder.Property(x => x.CapturedAt).HasColumnName("captured_at").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Ignore(x => x.IsMaterialized);

        builder.HasIndex(x => x.TaskRunId).HasDatabaseName("ix_ev_run");
        builder.HasIndex(x => x.ExecutionId).HasDatabaseName("ix_ev_exec");
        builder.HasIndex(x => x.RequirementId).HasDatabaseName("ix_ev_req");

        builder.HasIndex(x => x.CapturedAt)
            .HasDatabaseName("ix_ev_time")
            .IsDescending();

        // A given file materializes at most one live piece of evidence.
        builder.HasIndex(x => x.FileId)
            .IsUnique()
            .HasDatabaseName("ux_ev_file")
            .HasFilter($"file_id IS NOT NULL AND {ExecutionConfigurationExtensions.LiveRowsFilter}");
    }
}
