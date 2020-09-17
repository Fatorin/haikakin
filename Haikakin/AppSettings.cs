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
        public string UserSecret { get; set; }
        public string EmailSecretHashKey { get; set; }
        public string EmailSecretHashIV { get; set; }
        public string MailgunAPIKey { get; set; }
        public string NewebPayMerchantID { get; set; }
        public string NewebPayHashKey { get; set; }
        public string NewebPayHashIV { get; set; }
        public string EzPayMerchantID { get; set; }
        public string EzPayHashKey { get; set; }
        public string EzPayHashIV { get; set; }
        public string SerialHashKey { get; set; }
        public string SerialHashIV { get; set; }
    }
}
