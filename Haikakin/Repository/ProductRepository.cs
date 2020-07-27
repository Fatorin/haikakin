using Haikakin.Data;
using Haikakin.Models;
using Haikakin.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _db;

        public ProductRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public bool CreateProduct(Product product)
        {
            _db.Products.Add(product);
            return Save();
        }

        public bool DeleteProduct(Product product)
        {
            _db.Products.Remove(product);
            return Save();
        }

        public Product GetProduct(int ProductId)
        {
            return _db.Products.FirstOrDefault(u => u.ProductId == ProductId);
        }

        public ICollection<Product> GetProducts()
        {
            return _db.Products.OrderBy(u => u.ProductId).ToList();
        }

        public bool ProductExists(int id)
        {
            bool value = _db.Products.Any(u => u.ProductId == id);
            return value;
        }

        public bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }

        public bool UpdateProduct(Product Product)
        {
            _db.Products.Update(Product);
            return Save();
        }
    }
}
