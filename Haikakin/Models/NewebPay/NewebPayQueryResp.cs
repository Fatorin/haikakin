using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.NewebPay
{
    public class NewebPayQueryResp
    {
        [StringLength(10)]
        public string Status { get; set; }

        [StringLength(30)]
        public string Message { get; set; }
        public string MerchantID { get; set; }
        public int Amt { get; set; }
        public string TradeNo { get; set; }
        public string MerchantOrderNo { get; set; }
        public int TradeStatus { get; set; }
        public string PaymentType { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime PayTime { get; set; }
        public string CheckCode { get; set; }
        public string FundTime { get; set; }
        public string PayInfo { get; set; }
        public string ExpireDate { get; set; }
    }
}
