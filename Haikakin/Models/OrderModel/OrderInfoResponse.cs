﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.OrderModel
{
    public class OrderInfoResponse
    {
        public string Name { get; set; }
        public int Count { get; set; }

        public OrderInfoResponse(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }
}
