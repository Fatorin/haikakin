using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Haikakin.Extension
{
    public static class Encrypt
    {
        public static string HMACSHA256(this string message)
        {
            var encoding = new System.Text.UTF8Encoding();
            var key = "s3q13Aa6P4oijr1B3A2L0p8";
            byte[] keyByte = encoding.GetBytes(key);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacSHA256 = new HMACSHA256(keyByte))
            {
                byte[] hashMessage = hmacSHA256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
            }
        }
    }
}
