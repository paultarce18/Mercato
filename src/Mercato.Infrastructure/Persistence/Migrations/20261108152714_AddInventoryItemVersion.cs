using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryItemVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "inventory",
                table: "InventoryItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "inventory",
                table: "InventoryItems");
        }
    }
}
