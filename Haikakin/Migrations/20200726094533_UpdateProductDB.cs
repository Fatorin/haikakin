using Microsoft.EntityFrameworkCore.Migrations;

namespace Haikakin.Migrations
{
    public partial class UpdateProductDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "item_order",
                table: "products",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "item_type",
                table: "products",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "item_order",
                table: "products");

            migrationBuilder.DropColumn(
                name: "item_type",
                table: "products");
        }
    }
}
