using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.NewebPay
{
    public class NewebPayReceipt
    {
        public string RespondType { get; set; }
        public string Version { get; set; }
        public string TimeStamp { get; set; }
        public string TransNum { get; set; }
        public string MerchantOrderNo { get; set; }
        public string Status { get; set; }
        public string CreateStatusTime { get; set; }
        public string Category { get; set; }
        public string BuyerName { get; set; }
        public string BuyerUBN { get; set; }
        public string BuyerAddress { get; set; }
        public string BuyerEmail { get; set; }
        public string CarrierType { get; set; }
        public string CarrierNum { get; set; }
        public int? LoveCode { get; set; }
        public string PrintFlag { get; set; }
        public string TaxType { get; set; }
        public float TaxRate { get; set; }
        public string CustomsClearance { get; set; }
        public int Amt { get; set; }
        public int? AmtSales { get; set; }
        public int? AmtZero { get; set; }
        public int? AmtFree { get; set; }
        public int TaxAmt { get; set; }
        public int TotalAmt { get; set; }
        public string ItemName { get; set; }
        public int ItemCount { get; set; }
        public string ItemUnit { get; set; }
        public int ItemPrice { get; set; }
        public int ItemAmt { get; set; }
        public int? ItemTaxType { get; set; }
        public string Comment { get; set; }
    }
}