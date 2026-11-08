using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "ordering",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "ordering",
                table: "Orders");
        }
    }
}
