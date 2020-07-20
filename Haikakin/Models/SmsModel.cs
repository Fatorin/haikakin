using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class SmsModel
    {
        [Key]
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
    }
}
