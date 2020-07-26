using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class OrderInfo
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderTime { get; set; }

        public int Count { get; set; }

        [Required]
        public int ProductId { get; set; }


        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Required]
        [ForeignKey("OrderId")]
        public int OrderId { get; set; }
    }
}
