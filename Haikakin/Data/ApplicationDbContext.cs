using Haikakin.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Data
{
    public class ApplicationDbContext : DbContext
    {
        const int InitUserSql = 10001000;
        const int InitOrderSql = 20001000;
        const int InitProductSql = 30001000;
        const int InitSmsSql = 40001000;
        const int InitOrderInfoSql = 50001000;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().Property(u => u.UserId).HasIdentityOptions(startValue: InitUserSql).ValueGeneratedOnAdd();
            modelBuilder.Entity<Order>().Property(u => u.OrderId).HasIdentityOptions(startValue: InitOrderSql).ValueGeneratedOnAdd();
            modelBuilder.Entity<Product>().Property(u => u.ProductId).HasIdentityOptions(startValue: InitProductSql).ValueGeneratedOnAdd();
            modelBuilder.Entity<SmsModel>().Property(u => u.SmsId).HasIdentityOptions(startValue: InitSmsSql).ValueGeneratedOnAdd();
            modelBuilder.Entity<OrderInfo>().Property(u => u.OrderInfoId).HasIdentityOptions(startValue: InitOrderInfoSql).ValueGeneratedOnAdd();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderInfo> OrderInfos { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<SmsModel> SmsModels { get; set; }
    }
}
