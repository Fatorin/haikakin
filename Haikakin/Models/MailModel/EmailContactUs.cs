using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.EmailVerityModel;

namespace Haikakin.Models.MailModel
{
    public class EmailContactUs
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string TradeNo { get; set; }
        public string Context { get; set; }
    }
}
