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
        public string MailgunAPIKey { get; set; }
        public string ECPayMerchantID { get; set; }
        public string ECPayHashKey { get; set; }
        public string ECPayHashIV { get; set; }
        public string NewebPayMerchantID { get; set; }
        public string NewebPayHashKey { get; set; }
        public string NewebPayHashIV { get; set; }
        public string SerialHashKey { get; set; }
        public string SerialHashIV { get; set; }
    }
}
