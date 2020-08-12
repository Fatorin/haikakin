using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haikakin.Models.ECPayModel
{
    public class ECPaymentModel : ECPayCVSExtendArguments
    {
        /// <summary>
        /// 建立綠界訂單
        /// </summary>
        /// <param name="merchantTradeNo">訂單編號</param>
        /// <param name="totalAmount">總金額</param>
        /// <param name="tradeDesc">交易在綠界上的說明</param>
        /// <param name="itemNameList">交易商品的名稱清單</param>
        /// <param name="description">交易在便利商店繳費的敘述</param>
        public ECPaymentModel(string merchantTradeNo, int totalAmount, string tradeDesc, List<string> itemNameList,string description)
        {
            this.MerchantID = "2000214";
            this.MerchantTradeNo = merchantTradeNo;
            this.MerchantTradeDate = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss");
            this.TotalAmount = totalAmount;
            this.TradeDesc = tradeDesc;
            var sb = new StringBuilder();
            for (int i = 0; i < itemNameList.Count; i++)
            {
                if (i != itemNameList.Count - 1)
                {
                    sb.Append($"{itemNameList[i]}#");
                }
                else
                {
                    sb.Append($"{itemNameList[i]}");
                }
            }
            this.ItemName = sb.ToString();
            this.ReturnURL = $"https://www.haikakin.com/api/v1/Orders/FinishOrder";
            this.ClientBackURL = $"https://www.haikakin.com/account/order";
            this.ChoosePayment = ECPaymentMethod.CVS.ToString("G");
            this.StoreExpireDate = 15;
            this.Desc_1 = description;
        }

        [Required]
        [StringLength(10)]
        public string MerchantID { get; set; }

        [Required]
        [StringLength(20)]
        public string MerchantTradeNo { get; set; }

        [Required]
        [StringLength(20)]
        public string StoreID { get; set; }

        [Required]
        [StringLength(20)]
        public string MerchantTradeDate { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentType { get; set; } = "aio";

        [Required]
        public int TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string TradeDesc { get; set; }

        [Required]
        [StringLength(400)]
        public string ItemName { get; set; }

        [Required]
        [StringLength(200)]
        public string ReturnURL { get; set; }

        //請參考ECPaymentMethod
        [Required]
        [StringLength(20)]
        public string ChoosePayment { get; set; }

        [Required]
        public string CheckMacValue { get; set; }

        public string ClientBackURL { get; set; }

        public string ItemURL { get; set; }

        [StringLength(200)]
        public string Remark { get; set; }

        [Required]
        [StringLength(20)]
        public string ChooseSubPayment { get; set; }

        [StringLength(200)]
        public string OrderResultURL { get; set; }

        [StringLength(200)]
        public string NeedExtraPaidInfo { get; set; } = "N";

        [StringLength(1)]
        public string DeviceSource { get; set; }

        [StringLength(100)]
        public string IgnorePayment { get; set; }

        [StringLength(10)]
        public string PlatformID { get; set; }

        [StringLength(1)]
        public string InvoiceMark { get; set; }

        [StringLength(50)]
        public string CustomField1 { get; set; }

        [StringLength(50)]
        public string CustomField2 { get; set; }

        [StringLength(50)]
        public string CustomField3 { get; set; }

        [StringLength(50)]
        public string CustomField4 { get; set; }

        public int EncryptType { get; private set; } = 1;

        [StringLength(3)]
        public string Language { get; set; }

    }
}
