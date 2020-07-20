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
        public int Id { get; set; }
        public DateTime OrderTime { get; set; }
        [Required]
        public OrderStatusType OrderStatus { get; set; }
        [Required]
        public int OrderPrice { get; set; }

        public OrderPayType OrderPay { get; set; }
        [Required]
        public int UserId { get; set; }

        public UserDto User { get; set; }
    }
}
