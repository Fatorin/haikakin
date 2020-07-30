using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.ECPayModel
{
    public enum ECPaymentMethod
    {
        //
        // 摘要:
        //     不指定付款方式。
        ALL = 0,
        //
        // 摘要:
        //     信用卡付費。
        Credit = 1,
        //
        // 摘要:
        //     網路 ATM。
        WebATM = 2,
        //
        // 摘要:
        //     自動櫃員機。
        ATM = 3,
        //
        // 摘要:
        //     超商代碼。
        CVS = 4,
        //
        // 摘要:
        //     超商條碼。
        BARCODE = 5,
        //
        // 摘要:
        //     支付寶。
        Alipay = 6,
        //
        // 摘要:
        //     財付通。
        Tenpay = 7,
        //
        // 摘要:
        //     儲值消費。
        TopUpUsed = 8,
        //
        // 摘要:
        //     GooglePay
        GooglePay = 11
    }
}
