using System.ComponentModel.DataAnnotations;

namespace Haikakin.Models.NewebPay
{
    public class NewebPayReceiptBase
    {
        [Required]
        [StringLength(15)]
        public string MerchantID_ { get; set; }
        [Required]
        public string PostData_ { get; set; }
    }
}
