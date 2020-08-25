using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.OrderModel.Order;

namespace Haikakin.Models.QueryModel
{
    public class QueryOrder
    {
        public DateTime StartTime { get; set; }
        public DateTime LastTime { get; set; }
        /// NonPayment, AlreadyPaid, Over, Cancel ///
        public OrderStatusType OrderStatus { get; set; }


    }
}
