﻿using System;
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
        public int Id { get; set; }
        public DateTime OrderTime { get; set; }
        public enum OrderStatusType { NonPayment, AlreadyPaid, Over }
        [Required]
        public OrderStatusType OrderStatus { get; set; }
        [Required]
        public double OrderPrice { get; set; }
        public enum OrderPayType { None, GooglePay, ApplePay, LinePay, CVSBarCode, CreditCard, ATM, WebATM }
        [Required]
        public OrderPayType OrderPay { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
    }
}
