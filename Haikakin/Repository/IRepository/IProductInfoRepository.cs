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

        ICollection<ProductInfo> GetProductInfosByOrderInfoId(int orderInfoId);

        ProductInfo GetProductInfo(int productInfoId);

        bool ProductInfoExists(int id);

        bool ProductInfoSerialExists(int productId, string Serial);

        bool CreateProductInfo(ProductInfo productInfo);

        bool DeleteProductInfo(int productInfoId);

        bool UpdateProductInfo(ProductInfo productInfo);

        bool Save();
    }
}
