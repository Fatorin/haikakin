using Haikakin.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Repository.IRepository
{
    public interface IProductInfoRepository
    {
        ICollection<ProductInfo> GetProductInfos();

        ProductInfo GetProductInfo(int productInfoId);

        bool ProductInfoExists(int id);

        bool CreateProductInfo(ProductInfo productInfo);

        bool UpdateProductInfo(ProductInfo productInfo);

        bool Save();
    }
}
