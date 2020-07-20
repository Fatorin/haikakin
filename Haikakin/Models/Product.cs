﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string ProductName { get; set; }
        [Required]
        public string Price { get; set; }
        public bool CanBuy { get; set; }
        public string Stock { get; set; }
        public byte[] Image { get; set; }
        public string Description { get; set; }
        public int Limit { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
}
