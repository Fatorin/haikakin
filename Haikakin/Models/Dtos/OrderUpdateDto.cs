using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.Order;

namespace Haikakin.Models.Dtos
{
    public class OrderUpdateDto
    {
        public int OrderId { get; set; }
        public int OrderPaySerial { get; set; }
        public OrderPayType OrderPay { get; set; }
    }
}
