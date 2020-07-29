using Microsoft.EntityFrameworkCore.Migrations;

namespace Haikakin.Migrations
{
    public partial class InitDB3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "order_pay",
                table: "orders");

            migrationBuilder.AddColumn<string>(
                name: "order_check_code",
                table: "orders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "order_pay_way",
                table: "orders",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "order_check_code",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_pay_way",
                table: "orders");

            migrationBuilder.AddColumn<int>(
                name: "order_pay",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
