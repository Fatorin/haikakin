using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.Dtos
{
    public class SmsModelDto
    {
        public string PhoneNumber { get; set; }
        public bool isTaiwanNumber { get; set; }
    }
}
