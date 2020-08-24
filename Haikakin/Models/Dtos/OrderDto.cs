using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.OrderModel.Order;

namespace Haikakin.Models.Dtos
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public DateTime OrderCreateTime { get; set; }
        public DateTime OrderLastUpdateTime { get; set; }
        public OrderStatusType OrderStatus { get; set; }
        public decimal OrderAmount { get; set; }
        public OrderPayWayEnum OrderPayWay { get; set; }
        public string OrderPaySerial { get; set; }
    }
}
