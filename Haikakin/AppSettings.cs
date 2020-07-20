using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin
{
    public class AppSettings
    {
        public string JwtSecret { get; set; }
        public string FacebookAppId { get; set; }
        public string FacebookAppSecret { get; set; }
        public string SmsAccountID { get; set; }
        public string SmsAccountPassword { get; set; }

    }
}
