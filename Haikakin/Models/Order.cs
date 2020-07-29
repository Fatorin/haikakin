using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public DateTime OrderCreateTime { get; set; }
        public DateTime OrderLastUpdateTime { get; set; }
        public enum OrderStatusType { NonPayment, AlreadyPaid, Over, Cancel }
        [Required]
        public OrderStatusType OrderStatus { get; set; }
        [Required]
        public double OrderPrice { get; set; }
        public enum OrderPayWayEnum { None, GooglePay, ApplePay, LinePay, CVSBarCode, CreditCard, ATM, WebATM }
        [Required]
        public OrderPayWayEnum OrderPayWay { get; set; }
        public int OrderPaySerial { get; set; }
        public string OrderCheckCode { get; set; }
        public double Exchange { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }

        public ICollection<OrderInfo> OrderInfos { get; set; }
    }
}
