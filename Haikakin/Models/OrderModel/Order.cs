using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.OrderModel
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public DateTime OrderCreateTime { get; set; }
        public DateTime OrderLastUpdateTime { get; set; }
        public enum OrderStatusType { NotGetCVSCode, HasGotCVSCode, AlreadyPaid, Over, Cancel }
        [Required]
        public OrderStatusType OrderStatus { get; set; }
        [Required]
        public decimal OrderAmount { get; set; }
        public enum OrderPayWayEnum { CVSBarCode, GooglePay, ApplePay, LinePay, CreditCard, ATM, WebATM }
        [Required]
        public OrderPayWayEnum OrderPayWay { get; set; }
        public string OrderPaySerial { get; set; }
        public string OrderThirdPaySerial { get; set; }
        public string OrderCVSCode { get; set; }
        public string OrderFee { get; set; }
        public DateTime? OrderPayLimitTime { get; set; }
        public decimal Exchange { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }

        public ICollection<OrderInfo> OrderInfos { get; set; }
    }
}
