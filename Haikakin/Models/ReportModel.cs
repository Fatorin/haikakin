using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class ReportModel
    {
        public int OrderId { get; set; }
        public string OrderItems { get; set; }
        public string OrderCounts { get; set; }
        public string OrderAmounts { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string OrderStatus { get; set; }
        public string BuyDate { get; set; }
        public string PayDate { get; set; }
        public decimal OrderAllAmount { get; set; }
        public decimal Exchange { get; set; }
        public string PayWay { get; set; }
        public decimal PayAmount { get; set; }
        public decimal PayFee { get; set; }
    }
}
