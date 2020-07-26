using Microsoft.EntityFrameworkCore.Migrations;

namespace Haikakin.Migrations
{
    public partial class AdjustDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_infos_orders_order_id",
                table: "order_infos");

            migrationBuilder.DropForeignKey(
                name: "fk_orders_users_user_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_user_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_order_infos_order_id",
                table: "order_infos");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_orders_user_id",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_infos_order_id",
                table: "order_infos",
                column: "order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_order_infos_orders_order_id",
                table: "order_infos",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_users_user_id",
                table: "orders",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
