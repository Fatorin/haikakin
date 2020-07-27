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
    public class ProductInfoRepository : IProductInfoRepository
    {
        private readonly ApplicationDbContext _db;

        public ProductInfoRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public bool CreateProductInfo(ProductInfo productInfo)
        {
            _db.ProductInfos.Add(productInfo);
            var product = _db.Products.FirstOrDefault(u => u.ProductId == productInfo.ProductId);
            product.Stock += 1;
            _db.Products.Update(product);
            return Save();
        }

        public bool UpdateProductInfo(ProductInfo productInfo)
        {
            _db.ProductInfos.Update(productInfo);
            return Save();
        }

        public ProductInfo GetProductInfo(int productInfoId)
        {
            return _db.ProductInfos.SingleOrDefault(p => p.ProductInfoId == productInfoId);
        }

        public ICollection<ProductInfo> GetProductInfos()
        {
            return _db.ProductInfos.OrderBy(p => p.ProductInfoId).ToList();
        }

        public bool ProductInfoExists(int id)
        {
            bool value = _db.ProductInfos.Any(u => u.ProductInfoId == id);
            return value;
        }

        public bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }
    }
}
