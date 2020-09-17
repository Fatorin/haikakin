using System;
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
        public string OrderThirdPaySerial { get; set; }
        public string OrderCVSCode { get; set; }
        public string OrderFee { get; set; }
        public DateTime? OrderPayLimitTime { get; set; }
        public CarrierTypeEnum CarrierType { get; set; }
        public string CarrierNum { get; set; }
    }
}
