using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
    }
}
