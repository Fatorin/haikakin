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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime OrderTime { get; set; }

        public enum OrderStatusType { NonPayment, AlreadyPaid, Over }
        [Required]
        public OrderStatusType OrderStatus { get; set; }
        [Required]
        public int OrderPrice { get; set; }

        public enum OrderPayType { GooglePay, ApplePay, LinePay, CVSBarCode, CreditCard }
        [Required]
        public OrderPayType OrderPay { get; set; }
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
