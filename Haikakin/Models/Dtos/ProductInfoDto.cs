using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.ProductInfo;

namespace Haikakin.Models.Dtos
{
    public class ProductInfoDto
    {
        /**流水號*/
        [Required]
        public int ProductInfoId { get; set; }
        /**序號(須加密)*/
        public string Serial { get; set; }
        /**進貨價*/
        public decimal PrimeCost { get; set; }
        /**已使用、已鎖定、已使用*/
        public ProductStatusEnum ProductStatus { get; set; }
    }
}
