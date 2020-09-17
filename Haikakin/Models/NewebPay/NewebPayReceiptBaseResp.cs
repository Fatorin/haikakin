using System;
using System.ComponentModel.DataAnnotations;

namespace Haikakin.Models.NewebPay
{
    public class NewebPayReceiptBaseResp
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string MerchantID { get; set; }
        public string InvoiceTransNo { get; set; }
        public string MerchantOrderNo { get; set; }
        public int TotalAmt { get; set; }
        public string InvoiceNumber { get; set; }
        public string RandomNum { get; set; }
        public DateTime CreateTime { get; set; }
        public string CheckCode { get; set; }
        public string BarCode { get; set; }
        public string QRcodeL { get; set; }
        public string QRcodeR { get; set; }
        public string EndStr { get; set; }
    }
}
