using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.NewebPay
{
    public class NewebPayCVSResp
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string MerchantID { get; set; }
        public int Amt { get; set; }
        public string TradeNo { get; set; }
        public string MerchantOrderNo { get; set; }
        public string PaymentType { get; set; }
        public DateTime ExpireDate { get; set; }
        public string CodeNo { get; set; }

    }
}
