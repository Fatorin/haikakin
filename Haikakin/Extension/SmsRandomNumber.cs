using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Haikakin.Extension
{
    public static class SmsRandomNumber
    {
        public static string CreatedNumber()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Haikakin-");
            for (int i = 0; i < 6; i++)
            {
                sb.Append(Next(9));
            }

            return sb.ToString();
        }

        private static int Next(int max)
        {
            byte[] rb = new byte[4];
            RNGCryptoServiceProvider rngp = new RNGCryptoServiceProvider();
            rngp.GetBytes(rb);
            int value = BitConverter.ToInt32(rb, 0);
            value = value % (max + 1);
            if (value < 0) value = -value;
            return value;
        }
    }
}
