using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexo.WorkModel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WorkModelInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "work");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processes",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    profile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    output_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    output_uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    site_id = table.Column<Guid>(type: "uuid", nullable: true),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_policy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    skip_policy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    external_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "process_versions",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_no = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    version_major = table.Column<short>(type: "smallint", nullable: false),
                    version_minor = table.Column<short>(type: "smallint", nullable: false),
                    version_patch = table.Column<short>(type: "smallint", nullable: false),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    profile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    change_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    workload_sec = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_process_versions_process",
                        column: x => x.process_id,
                        principalSchema: "work",
                        principalTable: "processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_dependencies",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    predecessor_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    successor_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    lag_sec = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_dep_version",
                        column: x => x.process_version_id,
                        principalSchema: "work",
                        principalTable: "process_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    display_seq = table.Column<int>(type: "integer", nullable: false),
                    estimated_duration_sec = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    standard_duration_sec = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    progress_weight = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    responsible_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    suggested_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    completion_spec = table.Column<string>(type: "jsonb", nullable: true),
                    obligation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    evidence_policy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    required_evidence_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    min_evidence_count = table.Column<short>(type: "smallint", nullable: false),
                    required_capability = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    required_asset_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_milestone = table.Column<bool>(type: "boolean", nullable: false),
                    is_parallelizable = table.Column<bool>(type: "boolean", nullable: false),
                    is_repeatable = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_tasks_version",
                        column: x => x.process_version_id,
                        principalSchema: "work",
                        principalTable: "process_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_inputs",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    uom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    basis = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tolerance_pct = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    requires_traceability = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_inputs", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_inputs_task",
                        column: x => x.task_id,
                        principalSchema: "work",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_work_outbox_processed_on",
                schema: "work",
                table: "outbox_messages",
                column: "processed_on");

            migrationBuilder.CreateIndex(
                name: "ix_work_outbox_tenant_id",
                schema: "work",
                table: "outbox_messages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_versions_state",
                schema: "work",
                table: "process_versions",
                column: "state",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_process_versions_no",
                schema: "work",
                table: "process_versions",
                columns: new[] { "process_id", "version_no" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_process_versions_published",
                schema: "work",
                table: "process_versions",
                column: "process_id",
                unique: true,
                filter: "state = 'published' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_processes_profile",
                schema: "work",
                table: "processes",
                column: "profile",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_processes_code",
                schema: "work",
                table: "processes",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_task_dep_predecessor",
                schema: "work",
                table: "task_dependencies",
                column: "predecessor_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_dep_successor",
                schema: "work",
                table: "task_dependencies",
                column: "successor_task_id");

            migrationBuilder.CreateIndex(
                name: "ux_task_dep_edge",
                schema: "work",
                table: "task_dependencies",
                columns: new[] { "process_version_id", "predecessor_task_id", "successor_task_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_task_inputs_item_id",
                schema: "work",
                table: "task_inputs",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ux_task_inputs_task_item",
                schema: "work",
                table: "task_inputs",
                columns: new[] { "task_id", "item_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_responsible_role_id",
                schema: "work",
                table: "tasks",
                column: "responsible_role_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_tasks_version_code",
                schema: "work",
                table: "tasks",
                columns: new[] { "process_version_id", "code" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "work");

            migrationBuilder.DropTable(
                name: "task_dependencies",
                schema: "work");

            migrationBuilder.DropTable(
                name: "task_inputs",
                schema: "work");

            migrationBuilder.DropTable(
                name: "tasks",
                schema: "work");

            migrationBuilder.DropTable(
                name: "process_versions",
                schema: "work");

            migrationBuilder.DropTable(
                name: "processes",
                schema: "work");
        }
    }
}
