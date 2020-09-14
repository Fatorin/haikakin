using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Haikakin.Extension.Services
{
    public static class ExchangeParseService
    {
        public static decimal GetExchange()
        {
            try
            {
                HtmlWeb webClient = new HtmlWeb(); //建立htmlweb
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                var uri = new Uri("https://www.apps1.asiapacific.hsbc.com/1/2/Misc/popup-tw/currency-calculator");
                HtmlDocument doc = webClient.Load(uri); //載入網址資料
                IEnumerable<HtmlNode> nodes = doc.DocumentNode.Descendants(0).Where(n => n.HasClass("ForRatesColumn02")); //抓取Xpath資料

                var limit = 0;
                foreach (var node in nodes)
                {
                    limit++;
                    if (limit == 2) return decimal.Parse(node.InnerText);
                }
                return 0;
            }
            catch (WebException)
            {
                return 0;
            }
        }

    }
}
