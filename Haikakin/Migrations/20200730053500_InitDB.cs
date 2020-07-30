using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Haikakin.Migrations
{
    public partial class InitDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    order_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'20001000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_create_time = table.Column<DateTime>(nullable: false),
                    order_last_update_time = table.Column<DateTime>(nullable: false),
                    order_status = table.Column<int>(nullable: false),
                    order_price = table.Column<decimal>(nullable: false),
                    order_pay_way = table.Column<int>(nullable: false),
                    order_pay_serial = table.Column<int>(nullable: false),
                    order_check_code = table.Column<string>(nullable: true),
                    exchange = table.Column<decimal>(nullable: false),
                    user_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.order_id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    product_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'30001000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_name = table.Column<string>(nullable: false),
                    price = table.Column<decimal>(nullable: false),
                    can_buy = table.Column<bool>(nullable: false),
                    stock = table.Column<int>(nullable: false),
                    image = table.Column<byte[]>(nullable: true),
                    description = table.Column<string>(nullable: true),
                    limit = table.Column<int>(nullable: false),
                    last_update_time = table.Column<DateTime>(nullable: false),
                    item_type = table.Column<int>(nullable: false),
                    item_order = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "sms_models",
                columns: table => new
                {
                    sms_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'40001000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    phone_number = table.Column<string>(nullable: false),
                    verity_code = table.Column<string>(nullable: false),
                    is_used = table.Column<bool>(nullable: false),
                    verity_limit_time = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sms_models", x => x.sms_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10001000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(nullable: false),
                    password = table.Column<string>(nullable: true),
                    email = table.Column<string>(nullable: true),
                    email_verity = table.Column<bool>(nullable: false),
                    phone_number = table.Column<string>(nullable: true),
                    phone_number_verity = table.Column<bool>(nullable: false),
                    ip_address = table.Column<string>(nullable: true),
                    role = table.Column<string>(nullable: true),
                    last_login_time = table.Column<DateTime>(nullable: false),
                    create_time = table.Column<DateTime>(nullable: false),
                    login_type = table.Column<int>(nullable: false),
                    cancel_times = table.Column<int>(nullable: false),
                    check_ban = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "order_infos",
                columns: table => new
                {
                    order_info_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'50001000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_time = table.Column<DateTime>(nullable: false),
                    count = table.Column<int>(nullable: false),
                    product_id = table.Column<int>(nullable: false),
                    order_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_infos", x => x.order_info_id);
                    table.ForeignKey(
                        name: "fk_order_infos_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token = table.Column<string>(nullable: true),
                    expires = table.Column<DateTime>(nullable: false),
                    created = table.Column<DateTime>(nullable: false),
                    created_by_ip = table.Column<string>(nullable: true),
                    revoked = table.Column<DateTime>(nullable: true),
                    revoked_by_ip = table.Column<string>(nullable: true),
                    replaced_by_token = table.Column<string>(nullable: true),
                    user_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_infos",
                columns: table => new
                {
                    product_info_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'60001000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    serial = table.Column<string>(nullable: true),
                    last_update_time = table.Column<DateTime>(nullable: false),
                    product_status = table.Column<int>(nullable: false),
                    order_info_id = table.Column<int>(nullable: true),
                    product_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_infos", x => x.product_info_id);
                    table.ForeignKey(
                        name: "fk_product_infos_order_infos_order_info_id",
                        column: x => x.order_info_id,
                        principalTable: "order_infos",
                        principalColumn: "order_info_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_infos_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_infos_order_id",
                table: "order_infos",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_infos_order_info_id",
                table: "product_infos",
                column: "order_info_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_infos_product_id",
                table: "product_infos",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_user_id",
                table: "RefreshToken",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_infos");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "sms_models");

            migrationBuilder.DropTable(
                name: "order_infos");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}
