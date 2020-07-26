using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Haikakin.Migrations
{
    public partial class CreateOrderInfoDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "limit_pay_time",
                table: "products",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<double>(
                name: "order_price",
                table: "orders",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "order_infos",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'50001000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_time = table.Column<DateTime>(nullable: false),
                    count = table.Column<int>(nullable: false),
                    product_id = table.Column<int>(nullable: false),
                    order_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_infos", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_infos_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_infos_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_infos_order_id",
                table: "order_infos",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_infos_product_id",
                table: "order_infos",
                column: "product_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_infos");

            migrationBuilder.DropColumn(
                name: "limit_pay_time",
                table: "products");

            migrationBuilder.AlterColumn<int>(
                name: "order_price",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(double));
        }
    }
}
