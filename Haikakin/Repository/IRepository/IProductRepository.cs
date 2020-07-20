using Haikakin.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Repository.IRepository
{
    public interface IProductRepository
    {
        ICollection<Product> GetProducts();

        Product GetProduct(int ProductId);

        bool ProductExists(int id);

        bool CreateProduct(Product Product);

        bool UpdateProduct(Product Product);

        bool DeleteProduct(Product Product);

        bool Save();
    }
}
