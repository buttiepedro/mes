using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexo.Production.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OutboxToProductionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "platform",
                newName: "outbox_messages",
                newSchema: "production");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "production",
                newName: "outbox_messages",
                newSchema: "platform");
        }
    }
}
