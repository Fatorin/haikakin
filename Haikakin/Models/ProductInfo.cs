using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Haikakin.Models
{
    public class ProductInfo
    {
        /**流水號*/
        [Key]
        public int ProductInfoId { get; set; }
        /**序號(須加密)*/
        public string Serial { get; set; }
        /**流水號*/
        public DateTime LastUpdateTime { get; set; }
        /**已使用、已鎖定、已使用*/
        public enum ProductStatusEnum { NotUse, Lock, Used }
        /**對應的訂單編號*/
        [ForeignKey("OrderInfoId")]
        public int OrderInfoId { get; set; }
    }
}
