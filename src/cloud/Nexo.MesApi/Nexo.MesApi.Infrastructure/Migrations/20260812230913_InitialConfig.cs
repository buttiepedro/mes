using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexo.MesApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "config");

            migrationBuilder.CreateTable(
                name: "cameras",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StreamUrl = table.Column<string>(type: "text", nullable: false),
                    Transport = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Fps = table.Column<int>(type: "integer", nullable: false),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AdjacentCameras = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cameras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "detection_classes",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detection_classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "location_nodes",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Level = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_location_nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rules",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ScopeLocationNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Trigger = table.Column<string>(type: "jsonb", nullable: false),
                    Emit = table.Column<string>(type: "jsonb", nullable: false),
                    CooldownSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "signal_devices",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Protocol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Config = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signal_devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "signals",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MqttTopic = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    JsonPath = table.Column<string>(type: "text", nullable: true),
                    ValueType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    Persistence = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vision_models",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ArtifactRef = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ProvidesClasses = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vision_models", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "zones",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CameraId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Polygon = table.Column<string>(type: "jsonb", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cameras_Code",
                schema: "config",
                table: "cameras",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detection_classes_Kind_Code",
                schema: "config",
                table: "detection_classes",
                columns: new[] { "Kind", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_location_nodes_ParentId_Code",
                schema: "config",
                table: "location_nodes",
                columns: new[] { "ParentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rules_Code",
                schema: "config",
                table: "rules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_signal_devices_Code",
                schema: "config",
                table: "signal_devices",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_signals_DeviceId_Code",
                schema: "config",
                table: "signals",
                columns: new[] { "DeviceId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_zones_CameraId_Code",
                schema: "config",
                table: "zones",
                columns: new[] { "CameraId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cameras",
                schema: "config");

            migrationBuilder.DropTable(
                name: "detection_classes",
                schema: "config");

            migrationBuilder.DropTable(
                name: "location_nodes",
                schema: "config");

            migrationBuilder.DropTable(
                name: "rules",
                schema: "config");

            migrationBuilder.DropTable(
                name: "signal_devices",
                schema: "config");

            migrationBuilder.DropTable(
                name: "signals",
                schema: "config");

            migrationBuilder.DropTable(
                name: "vision_models",
                schema: "config");

            migrationBuilder.DropTable(
                name: "zones",
                schema: "config");
        }
    }
}
