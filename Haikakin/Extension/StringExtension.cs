using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Haikakin.Models.OrderModel.Order;

namespace Haikakin.Extension
{
    public static class StringExtension
    {
        /// <summary>
        /// 將16進位字串轉換為byteArray
        /// </summary>
        /// <param name="source">欲轉換之字串</param>
        /// <returns></returns>
        public static byte[] ToByteArray(this string source)
        {
            byte[] result = null;

            if (!string.IsNullOrWhiteSpace(source))
            {
                var outputLength = source.Length / 2;
                var output = new byte[outputLength];

                for (var i = 0; i < outputLength; i++)
                {
                    output[i] = Convert.ToByte(source.Substring(i * 2, 2), 16);
                }
                result = output;
            }

            return result;
        }

        public static string SerialEncrypt(this string source)
        {
            StringBuilder sb = new StringBuilder(source);

            string rgxStr = @"\-";
            Regex rgx = new Regex(rgxStr);
            var matches = rgx.Matches(sb.ToString());
            if (matches.Count < 0)
                return null;
            for (int i = 0; i < matches.Count - 1; i++)
            {
                int len = matches[i + 1].Index - matches[i].Index - 1;
                int adjustStart = matches[i].Index + 1;
                sb.Remove(adjustStart, len);
                sb.Insert(adjustStart, "*", len);
            }

            return sb.ToString();
        }

        public static string GenRandomPassword(this int length)
        {
            var str = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!@#$%^&*()_+";
            var next = new Random();
            var builder = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                builder.Append(str[next.Next(0, str.Length)]);
            }

            return builder.ToString();
        }

        public static bool CheckCarrierFormat(CarrierTypeEnum carrierType, string carrierNum)
        {
            string regexPhone = @"^[/]{1}[A-Z0-9+-.]{7}$";
            string regexMoica = @"^[A-Z]{2}[0-9]{14}$";
            string regexLove = @"^[0-9]{3,7}$";

            switch (carrierType)
            {
                case CarrierTypeEnum.None:
                    return string.IsNullOrEmpty(carrierNum);

                case CarrierTypeEnum.Phone:
                    Regex rgxPhone = new Regex(regexPhone);
                    return rgxPhone.IsMatch(carrierNum);

                case CarrierTypeEnum.Moica:
                    Regex rgxMoica = new Regex(regexMoica);
                    return rgxMoica.IsMatch(carrierNum);

                case CarrierTypeEnum.Love:
                    Regex rgxLove = new Regex(regexLove);
                    return rgxLove.IsMatch(carrierNum);

                default:
                    return false;
            }
        }
    }
}