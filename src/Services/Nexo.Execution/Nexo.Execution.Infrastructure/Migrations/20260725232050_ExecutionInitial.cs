using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexo.Execution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "execution");

            migrationBuilder.CreateTable(
                name: "executions",
                schema: "execution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    process_id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_no = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    flavor = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    trigger_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    trigger_ref_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    trigger_ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trigger_external_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    target_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    target_uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    good_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    reject_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    deliverable = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    deliverable_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    committed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    contract_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    acceptance_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    site_id = table.Column<Guid>(type: "uuid", nullable: true),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    progress_pct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    progress_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actual_start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    close_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    close_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_executions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "execution",
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
                name: "evidence",
                schema: "execution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    media_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    content_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    hash_algo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    captured_by = table.Column<Guid>(type: "uuid", nullable: true),
                    caption = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence", x => x.id);
                    table.ForeignKey(
                        name: "fk_ev_exec",
                        column: x => x.execution_id,
                        principalSchema: "execution",
                        principalTable: "executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "input_consumptions",
                schema: "execution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    task_input_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    uom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    serial_id = table.Column<Guid>(type: "uuid", nullable: true),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_input_consumptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_ic_exec",
                        column: x => x.execution_id,
                        principalSchema: "execution",
                        principalTable: "executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_runs",
                schema: "execution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurrence = table.Column<short>(type: "smallint", nullable: false),
                    is_ad_hoc = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    assigned_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assignment_mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    work_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    std_duration_sec = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    est_duration_sec = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    progress_weight = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    actual_start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_setup_sec = table.Column<long>(type: "bigint", nullable: false),
                    actual_exec_sec = table.Column<long>(type: "bigint", nullable: false),
                    actual_wait_sec = table.Column<long>(type: "bigint", nullable: false),
                    actual_control_sec = table.Column<long>(type: "bigint", nullable: false),
                    actual_closing_sec = table.Column<long>(type: "bigint", nullable: false),
                    progress_pct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    progress_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    produced_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    target_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    is_on_critical_path = table.Column<bool>(type: "boolean", nullable: false),
                    is_milestone = table.Column<bool>(type: "boolean", nullable: false),
                    milestone_committed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    milestone_reached_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    obligation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    required_evidence_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    min_evidence_count = table.Column<short>(type: "smallint", nullable: false),
                    blocked_reason_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    blocked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_forced_close = table.Column<bool>(type: "boolean", nullable: false),
                    skip_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    close_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_runs_exec",
                        column: x => x.execution_id,
                        principalSchema: "execution",
                        principalTable: "executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_run_precedences",
                schema: "execution",
                columns: table => new
                {
                    predecessor_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    lag_sec = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_run_precedences", x => new { x.task_run_id, x.predecessor_task_id });
                    table.ForeignKey(
                        name: "FK_task_run_precedences_task_runs_task_run_id",
                        column: x => x.task_run_id,
                        principalSchema: "execution",
                        principalTable: "task_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ev_exec",
                schema: "execution",
                table: "evidence",
                column: "execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_ev_req",
                schema: "execution",
                table: "evidence",
                column: "requirement_id");

            migrationBuilder.CreateIndex(
                name: "ix_ev_run",
                schema: "execution",
                table: "evidence",
                column: "task_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_ev_time",
                schema: "execution",
                table: "evidence",
                column: "captured_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ux_ev_file",
                schema: "execution",
                table: "evidence",
                column: "file_id",
                unique: true,
                filter: "file_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_exec_committed",
                schema: "execution",
                table: "executions",
                column: "committed_date",
                filter: "flavor = 'project' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_exec_customer",
                schema: "execution",
                table: "executions",
                column: "customer_id",
                filter: "customer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_exec_flavor_status",
                schema: "execution",
                table: "executions",
                columns: new[] { "flavor", "status" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_exec_status",
                schema: "execution",
                table: "executions",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_exec_trigger",
                schema: "execution",
                table: "executions",
                columns: new[] { "trigger_ref_kind", "trigger_ref_id" });

            migrationBuilder.CreateIndex(
                name: "ix_exec_version",
                schema: "execution",
                table: "executions",
                column: "process_version_id");

            migrationBuilder.CreateIndex(
                name: "ux_exec_code",
                schema: "execution",
                table: "executions",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ic_batch",
                schema: "execution",
                table: "input_consumptions",
                column: "batch_id",
                filter: "batch_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ic_exec",
                schema: "execution",
                table: "input_consumptions",
                column: "execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_ic_item_time",
                schema: "execution",
                table: "input_consumptions",
                columns: new[] { "item_id", "recorded_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ic_run",
                schema: "execution",
                table: "input_consumptions",
                column: "task_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_execution_outbox_processed_on",
                schema: "execution",
                table: "outbox_messages",
                column: "processed_on");

            migrationBuilder.CreateIndex(
                name: "ix_execution_outbox_tenant_id",
                schema: "execution",
                table: "outbox_messages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_runs_exec",
                schema: "execution",
                table: "task_runs",
                column: "execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_runs_milestone",
                schema: "execution",
                table: "task_runs",
                columns: new[] { "execution_id", "milestone_committed_date" },
                filter: "is_milestone");

            migrationBuilder.CreateIndex(
                name: "ix_task_runs_person",
                schema: "execution",
                table: "task_runs",
                columns: new[] { "assigned_person_id", "status" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_task_runs_status",
                schema: "execution",
                table: "task_runs",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_task_runs_wc_time",
                schema: "execution",
                table: "task_runs",
                columns: new[] { "work_center_id", "actual_start_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_task_runs_instance",
                schema: "execution",
                table: "task_runs",
                columns: new[] { "execution_id", "task_id", "occurrence" },
                unique: true,
                filter: "deleted_at IS NULL AND task_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence",
                schema: "execution");

            migrationBuilder.DropTable(
                name: "input_consumptions",
                schema: "execution");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "execution");

            migrationBuilder.DropTable(
                name: "task_run_precedences",
                schema: "execution");

            migrationBuilder.DropTable(
                name: "task_runs",
                schema: "execution");

            migrationBuilder.DropTable(
                name: "executions",
                schema: "execution");
        }
    }
}
