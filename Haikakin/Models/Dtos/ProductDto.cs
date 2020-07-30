using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.Dtos
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public bool CanBuy { get; set; }
        public int Stock { get; set; }
        public byte[] Image { get; set; }
        public string Description { get; set; }
        public int Limit { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public DateTime LimitPayTime { get; set; }

        public int ItemType { get; set; }
        public int ItemOrder { get; set; }
    }
}
