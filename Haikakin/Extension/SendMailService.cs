using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Extension
{
    public class SendMailService
    {
        public enum SendAction { UserEmail, OrderCreateEmail, OrderFinshEmail }
        private readonly string _orderMailTitle = "Haikakin 訂單詳細資訊";
        private readonly string _userMailTitle = "Haikakin 會員驗證信";
        private string _mailApiKey;

        public SendMailService(string APIKey)
        {
            _mailApiKey = APIKey;
        }

        public bool SendMail(SendAction action, string[] sendInfos)
        {
            if (sendInfos.Length <= 0)
            {
                return false;
            }

            var mailBody = "<html>HTML version of the body</html>";
            var mailTilte = "";
            var userEmail = "";

            switch (action)
            {
                case SendAction.UserEmail:
                    //判斷SendInfos資訊，抓取玩家ID跟Email
                    var uId = sendInfos[0];
                    userEmail = sendInfos[1];
                    mailTilte = _userMailTitle;
                    var mailUrl = $"http://localhost:4200/mailverifcation?uid={uId}&email={userEmail}";
                    mailBody = $"<html>親愛的使用者，你的信箱驗證網址為: {mailUrl}</html>";
                    break;
                case SendAction.OrderCreateEmail:
                    //判斷SendInfos資訊
                    mailTilte = _orderMailTitle;
                    mailBody = $"<html>親愛的使用者，你的資料如下</html>";
                    break;
            }

            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");
            client.Authenticator =
                new HttpBasicAuthenticator("api", _mailApiKey);
            RestRequest request = new RestRequest();
            request.AddParameter("domain", "mail.haikakin.com", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", "Haikakin Service <service@mail.haikakin.com>");
            request.AddParameter("to", $"{userEmail}");
            request.AddParameter("subject", mailTilte);
            request.AddParameter("html", mailBody);
            request.Method = Method.POST;
            var response = client.Execute(request);
            return response.IsSuccessful;
        }
    }
}
