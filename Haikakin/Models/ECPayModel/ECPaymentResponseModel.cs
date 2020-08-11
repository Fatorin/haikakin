using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haikakin.Models.ECPayModel
{
    public class ECPaymentResponseModel
    {
        [StringLength(10)]
        public string MerchantID { get; set; }

        [StringLength(20)]
        public string MerchantTradeNo { get; set; }

        [StringLength(20)]
        public string StoreID { get; set; }

        /// <summary>
        /// 回傳1時代表付款成功。
        /// </summary>
        public int RtnCode { get; set; }

        [StringLength(200)]
        public string RtnMsg { get; set; }

        [StringLength(20)]
        public string TradeNo { get; set; }

        public int TradeAmt { get; set; }

        [StringLength(20)]
        public string PaymentDate { get; set; }

        [StringLength(20)]
        public string PaymentType { get; set; }

        public int PaymentTypeChargeFee { get; set; }

        [StringLength(20)]
        public string TradeDate { get; set; }

        public int SimulatePaid { get; set; }

        [StringLength(50)]
        public string CustomField1 { get; set; }

        [StringLength(50)]
        public string CustomField2 { get; set; }

        [StringLength(50)]
        public string CustomField3 { get; set; }

        [StringLength(50)]
        public string CustomField4 { get; set; }

        public string CheckMacValue { get; set; }

        public override string ToString()
        {
            return $"MerchantID={MerchantID}, MerchantTradeNo={MerchantTradeNo}, StoreID={StoreID},\n RtnCode={RtnCode}, RtnMsg={RtnMsg}, TradeNo={TradeNo},\n TradeAmt={TradeAmt}, PaymentDate={PaymentDate}, PaymentType={PaymentType},\n PaymentTypeChargeFee={PaymentTypeChargeFee}, TradeDate={TradeDate}, SimulatePaid={SimulatePaid},\n CustomField1={CustomField1}, CustomField2={CustomField2}, CustomField3={CustomField3},\n CustomField3={CustomField3}, CustomField4={CustomField4}, CheckMacValue={CheckMacValue}";
        }
    }
}
