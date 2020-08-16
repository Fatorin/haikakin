using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.Order;

namespace Haikakin.Models.OrderModel
{
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public DateTime OrderCreateTime { get; set; }
        public DateTime OrderLastUpdateTime { get; set; }
        public OrderStatusType OrderStatus { get; set; }
        public int OrderAmount { get; set; }
        public OrderPayWayEnum OrderPayWay { get; set; }
        public string OrderPaySerial { get; set; }
        public List<OrderInfoResponse> OrderInfos { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserIPAddress { get; set; }
        public string UserEmail { get; set; }
    }
}
