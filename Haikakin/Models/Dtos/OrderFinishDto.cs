﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.Order;

namespace Haikakin.Models.Dtos
{
    public class OrderFinishDto
    {
        public int OrderId { get; set; }
        public string OrderPaySerial { get; set; }
    }
}
