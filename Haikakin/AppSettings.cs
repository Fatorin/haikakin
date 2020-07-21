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
        public string MitakeSmsAccountID { get; set; }
        public string MitakeSmsAccountPassword { get; set; }
        public string TwilioSmsAccountID { get; set; }
        public string TwilioSmsAuthToken { get; set; }
        public string UserSecret { get; set; }
        public string EmailSecret { get; set; }

    }
}
