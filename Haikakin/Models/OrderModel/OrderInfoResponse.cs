using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.OrderModel
{
    public class OrderInfoResponse
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public List<string> Serials { get; set; }

        public OrderInfoResponse(string name, int count, List<string> serials)
        {
            Name = name;
            Count = count;
            Serials = serials;
        }
    }
}
