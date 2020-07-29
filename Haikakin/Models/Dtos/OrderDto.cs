using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.Order;

namespace Haikakin.Models.Dtos
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public DateTime OrderCreateTime { get; set; }
        public DateTime OrderLastUpdateTime { get; set; }
        public OrderStatusType OrderStatus { get; set; }
        public int OrderPrice { get; set; }
        public OrderPayType OrderPay { get; set; }
        public int OrderPaySerial { get; set; }
        public int UserId { get; set; }
        public ICollection<OrderInfo> OrderInfos { get; set; }
    }
}
