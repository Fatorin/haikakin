using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Haikakin.Extension.ECPay
{
    public static class ECPayCheckValue
    {
        public static string BuildCheckMacValue(string parameters, string hashKey, string hashIV, int encryptType = 0)
        {
            string szCheckMacValue = String.Empty;
            // 產生檢查碼。
            szCheckMacValue = String.Format("HashKey={0}{1}&HashIV={2}", hashKey, parameters, hashIV);
            szCheckMacValue = HttpUtility.UrlEncode(szCheckMacValue).ToLower();
            if (encryptType == 1)
            {
                szCheckMacValue = SHA256Encoder.Encrypt(szCheckMacValue);
            }
            else
            {
                szCheckMacValue = MD5Encoder.Encrypt(szCheckMacValue);
            }
            return szCheckMacValue;
        }
    }
}
