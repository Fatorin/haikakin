using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Haikakin.Extension
{
    public static class ExchangeParse
    {
        public static double GetExchange()
        {
            try
            {
                HtmlWeb webClient = new HtmlWeb(); //建立htmlweb
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var uri = new Uri("https://www.apps1.asiapacific.hsbc.com/1/2/Misc/popup-tw/currency-calculator");
                HtmlDocument doc = webClient.Load(uri); //載入網址資料
                IEnumerable<HtmlNode> nodes = doc.DocumentNode.Descendants(0).Where(n => n.HasClass("ForRatesColumn02")); //抓取Xpath資料

                var limit = 0;
                foreach (var node in nodes)
                {
                    limit++;
                    if (limit == 2) return double.Parse(node.InnerText);
                }
                return 0.00;
            }
            catch (Exception)
            {
                return 0;
            }
        }

    }
}
