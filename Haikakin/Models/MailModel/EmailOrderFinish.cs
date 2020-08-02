using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.MailModel
{
    public class EmailOrderFinish
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string OrderId { get; set; }
        public List<EmailOrderInfo> OrderItemList { get; set; }
    }
}
