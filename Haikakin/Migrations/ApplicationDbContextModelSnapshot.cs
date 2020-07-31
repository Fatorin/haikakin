﻿// <auto-generated />
using System;
using Haikakin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Haikakin.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Haikakin.Models.Order", b =>
                {
                    b.Property<int>("OrderId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("order_id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:IdentitySequenceOptions", "'20001000', '1', '', '', 'False', '1'")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<decimal>("Exchange")
                        .HasColumnName("exchange")
                        .HasColumnType("numeric");

                    b.Property<string>("OrderCheckCode")
                        .HasColumnName("order_check_code")
                        .HasColumnType("text");

                    b.Property<DateTime>("OrderCreateTime")
                        .HasColumnName("order_create_time")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime>("OrderLastUpdateTime")
                        .HasColumnName("order_last_update_time")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("OrderPaySerial")
                        .HasColumnName("order_pay_serial")
                        .HasColumnType("text");

                    b.Property<int>("OrderPayWay")
                        .HasColumnName("order_pay_way")
                        .HasColumnType("integer");

                    b.Property<decimal>("OrderPrice")
                        .HasColumnName("order_price")
                        .HasColumnType("numeric");

                    b.Property<int>("OrderStatus")
                        .HasColumnName("order_status")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("integer");

                    b.HasKey("OrderId")
                        .HasName("pk_orders");

                    b.ToTable("orders");
                });

            modelBuilder.Entity("Haikakin.Models.OrderInfo", b =>
                {
                    b.Property<int>("OrderInfoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("order_info_id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:IdentitySequenceOptions", "'50001000', '1', '', '', 'False', '1'")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("Count")
                        .HasColumnName("count")
                        .HasColumnType("integer");

                    b.Property<int>("OrderId")
                        .HasColumnName("order_id")
                        .HasColumnType("integer");

                    b.Property<DateTime>("OrderTime")
                        .HasColumnName("order_time")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("ProductId")
                        .HasColumnName("product_id")
                        .HasColumnType("integer");

                    b.HasKey("OrderInfoId")
                        .HasName("pk_order_infos");

                    b.HasIndex("OrderId")
                        .HasName("ix_order_infos_order_id");

                    b.ToTable("order_infos");
                });

            modelBuilder.Entity("Haikakin.Models.Product", b =>
                {
                    b.Property<int>("ProductId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("product_id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:IdentitySequenceOptions", "'30001000', '1', '', '', 'False', '1'")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("CanBuy")
                        .HasColumnName("can_buy")
                        .HasColumnType("boolean");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<byte[]>("Image")
                        .HasColumnName("image")
                        .HasColumnType("bytea");

                    b.Property<int>("ItemOrder")
                        .HasColumnName("item_order")
                        .HasColumnType("integer");

                    b.Property<int>("ItemType")
                        .HasColumnName("item_type")
                        .HasColumnType("integer");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnName("last_update_time")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("Limit")
                        .HasColumnName("limit")
                        .HasColumnType("integer");

                    b.Property<decimal>("Price")
                        .HasColumnName("price")
                        .HasColumnType("numeric");

                    b.Property<string>("ProductName")
                        .IsRequired()
                        .HasColumnName("product_name")
                        .HasColumnType("text");

                    b.Property<int>("Stock")
                        .HasColumnName("stock")
                        .HasColumnType("integer");

                    b.HasKey("ProductId")
                        .HasName("pk_products");

                    b.ToTable("products");
                });

            modelBuilder.Entity("Haikakin.Models.ProductInfo", b =>
                {
                    b.Property<int>("ProductInfoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("product_info_id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:IdentitySequenceOptions", "'60001000', '1', '', '', 'False', '1'")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnName("last_update_time")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int?>("OrderInfoId")
                        .HasColumnName("order_info_id")
                        .HasColumnType("integer");

                    b.Property<int>("ProductId")
                        .HasColumnName("product_id")
                        .HasColumnType("integer");

                    b.Property<int>("ProductStatus")
                        .HasColumnName("product_status")
                        .HasColumnType("integer");

                    b.Property<string>("Serial")
                        .HasColumnName("serial")
                        .HasColumnType("text");

                    b.HasKey("ProductInfoId")
                        .HasName("pk_product_infos");

                    b.HasIndex("OrderInfoId")
                        .HasName("ix_product_infos_order_info_id");

                    b.HasIndex("ProductId")
                        .HasName("ix_product_infos_product_id");

                    b.ToTable("product_infos");
                });

            modelBuilder.Entity("Haikakin.Models.SmsModel", b =>
                {
                    b.Property<int>("SmsId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("sms_id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:IdentitySequenceOptions", "'40001000', '1', '', '', 'False', '1'")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("IsUsed")
                        .HasColumnName("is_used")
                        .HasColumnType("boolean");

                    b.Property<string>("PhoneNumber")
                        .IsRequired()
                        .HasColumnName("phone_number")
                        .HasColumnType("text");

                    b.Property<string>("VerityCode")
                        .IsRequired()
                        .HasColumnName("verity_code")
                        .HasColumnType("text");

                    b.Property<DateTime>("VerityLimitTime")
                        .HasColumnName("verity_limit_time")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("SmsId")
                        .HasName("pk_sms_models");

                    b.ToTable("sms_models");
                });

            modelBuilder.Entity("Haikakin.Models.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("user_id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:IdentitySequenceOptions", "'10001000', '1', '', '', 'False', '1'")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("CancelTimes")
                        .HasColumnName("cancel_times")
                        .HasColumnType("integer");

                    b.Property<bool>("CheckBan")
                        .HasColumnName("check_ban")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnName("create_time")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Email")
                        .HasColumnName("email")
                        .HasColumnType("text");

                    b.Property<bool>("EmailVerity")
                        .HasColumnName("email_verity")
                        .HasColumnType("boolean");

                    b.Property<string>("IPAddress")
                        .HasColumnName("ip_address")
                        .HasColumnType("text");

                    b.Property<DateTime>("LastLoginTime")
                        .HasColumnName("last_login_time")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("LoginType")
                        .HasColumnName("login_type")
                        .HasColumnType("integer");

                    b.Property<string>("Password")
                        .HasColumnName("password")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnName("phone_number")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberVerity")
                        .HasColumnName("phone_number_verity")
                        .HasColumnType("boolean");

                    b.Property<string>("Role")
                        .HasColumnName("role")
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnName("username")
                        .HasColumnType("text");

                    b.HasKey("UserId")
                        .HasName("pk_users");

                    b.ToTable("users");
                });

            modelBuilder.Entity("Haikakin.Models.OrderInfo", b =>
                {
                    b.HasOne("Haikakin.Models.Order", null)
                        .WithMany("OrderInfos")
                        .HasForeignKey("OrderId")
                        .HasConstraintName("fk_order_infos_orders_order_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Haikakin.Models.ProductInfo", b =>
                {
                    b.HasOne("Haikakin.Models.OrderInfo", "OrderInfo")
                        .WithMany()
                        .HasForeignKey("OrderInfoId")
                        .HasConstraintName("fk_product_infos_order_infos_order_info_id");

                    b.HasOne("Haikakin.Models.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .HasConstraintName("fk_product_infos_products_product_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Haikakin.Models.User", b =>
                {
                    b.OwnsMany("Haikakin.Models.RefreshToken", "RefreshTokens", b1 =>
                        {
                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnName("id")
                                .HasColumnType("integer")
                                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                            b1.Property<DateTime>("Created")
                                .HasColumnName("created")
                                .HasColumnType("timestamp without time zone");

                            b1.Property<string>("CreatedByIp")
                                .HasColumnName("created_by_ip")
                                .HasColumnType("text");

                            b1.Property<DateTime>("Expires")
                                .HasColumnName("expires")
                                .HasColumnType("timestamp without time zone");

                            b1.Property<string>("ReplacedByToken")
                                .HasColumnName("replaced_by_token")
                                .HasColumnType("text");

                            b1.Property<DateTime?>("Revoked")
                                .HasColumnName("revoked")
                                .HasColumnType("timestamp without time zone");

                            b1.Property<string>("RevokedByIp")
                                .HasColumnName("revoked_by_ip")
                                .HasColumnType("text");

                            b1.Property<string>("Token")
                                .HasColumnName("token")
                                .HasColumnType("text");

                            b1.Property<int>("UserId")
                                .HasColumnName("user_id")
                                .HasColumnType("integer");

                            b1.HasKey("Id")
                                .HasName("pk_refresh_token");

                            b1.HasIndex("UserId")
                                .HasName("ix_refresh_token_user_id");

                            b1.ToTable("RefreshToken");

                            b1.WithOwner()
                                .HasForeignKey("UserId")
                                .HasConstraintName("fk_refresh_token_users_user_id");
                        });
                });
#pragma warning restore 612, 618
        }
    }
}
