using static Haikakin.Models.OrderModel.Order;

namespace Haikakin.Models.Dtos
{
    public class OrderCreateDto
    {
        public OrderCreateItems[] OrderCreateItems { get; set; }
        public CarrierTypeEnum CarrierType { get; set; }
        public string CarrierNum { get; set; }

    }

    public class OrderCreateItems
    {
        public int ProductId { get; set; }
        public int OrderCount { get; set; }
    }
}
