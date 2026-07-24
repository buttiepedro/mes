using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexo.MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MasterDataInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "master");

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    tax_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    contact = table.Column<string>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    governance = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    external_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "people",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    default_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    site_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    calendar = table.Column<string>(type: "jsonb", nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    governance = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    external_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_people", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "uom",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    magnitude = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    factor_to_base = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    is_base = table.Column<bool>(type: "boolean", nullable: false),
                    decimals = table.Column<short>(type: "smallint", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    governance = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    external_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uom", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    base_uom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    roles = table.Column<string[]>(type: "text[]", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    family = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    tracking = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ideal_cycle_time = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    default_process_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quality_specs = table.Column<string>(type: "jsonb", nullable: true),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    governance = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    external_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_items_uom",
                        column: x => x.base_uom_id,
                        principalSchema: "master",
                        principalTable: "uom",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_customers_code",
                schema: "master",
                table: "customers",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_customers_external_ref",
                schema: "master",
                table: "customers",
                column: "external_ref",
                unique: true,
                filter: "deleted_at IS NULL AND external_ref IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_items_base_uom_id",
                schema: "master",
                table: "items",
                column: "base_uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_roles",
                schema: "master",
                table: "items",
                column: "roles")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ux_items_code",
                schema: "master",
                table: "items",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_items_external_ref",
                schema: "master",
                table: "items",
                column: "external_ref",
                unique: true,
                filter: "deleted_at IS NULL AND external_ref IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "master",
                table: "outbox_messages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_TenantId",
                schema: "master",
                table: "outbox_messages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_people_site_id",
                schema: "master",
                table: "people",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ux_people_code",
                schema: "master",
                table: "people",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_people_external_ref",
                schema: "master",
                table: "people",
                column: "external_ref",
                unique: true,
                filter: "deleted_at IS NULL AND external_ref IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_people_user",
                schema: "master",
                table: "people",
                column: "user_id",
                unique: true,
                filter: "deleted_at IS NULL AND user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_uom_base_per_magnitude",
                schema: "master",
                table: "uom",
                column: "magnitude",
                unique: true,
                filter: "is_base AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_uom_code",
                schema: "master",
                table: "uom",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_uom_external_ref",
                schema: "master",
                table: "uom",
                column: "external_ref",
                unique: true,
                filter: "deleted_at IS NULL AND external_ref IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customers",
                schema: "master");

            migrationBuilder.DropTable(
                name: "items",
                schema: "master");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "master");

            migrationBuilder.DropTable(
                name: "people",
                schema: "master");

            migrationBuilder.DropTable(
                name: "uom",
                schema: "master");
        }
    }
}
