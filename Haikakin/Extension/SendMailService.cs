using Haikakin.Models;
using Haikakin.Models.MailModel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Haikakin.Extension
{
    public class SendMailService
    {
        private string _mailApiKey;

        public SendMailService(string APIKey)
        {
            _mailApiKey = APIKey;
        }

        private bool SendMailActive(EmailModel model)
        {
            if (model == null)
            {
                return false;
            }

            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");
            client.Authenticator =
                new HttpBasicAuthenticator("api", _mailApiKey);
            RestRequest request = new RestRequest();
            request.AddParameter("domain", "haikakin.com", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", "Haikakin Service <notification@haikakin.com>");
            request.AddParameter("to", $"{model.Email}");
            request.AddParameter("subject", model.EmailTitle);
            request.AddParameter("html", model.EmailBody);
            request.Method = Method.POST;
            var response = client.Execute(request);
            return response.IsSuccessful;
        }

        public bool AccountMailBuild(EmailAccount model)
        {
            var title = "Haikakin 會員驗證信";
            var url = $"http://www.haikakin.com/mailverifcation?uid={model.UserId}&email={model.UserEmail}&emailVerityAction={model.EmailVerityAction:d}";
            string body = File.ReadAllText(Path.Combine("EmailTemplates/Account.html"));
            body = body.Replace("#username", $"{model.UserName}");
            body = body.Replace("#url", url);
            body = body.Replace("#timespan", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));

            return SendMailActive(new EmailModel(model.UserEmail, title, body));
        }

        public bool OrderFinishMailBuild(EmailOrderFinish model)
        {
            var title = $"Haikakin 訂單編號:{model.OrderId} 購買完成內容";
            string body = File.ReadAllText(Path.Combine("EmailTemplates/Order.html"));
            var sb = new StringBuilder();

            foreach(var data in model.OrderItemList)
            {
                sb.Append($"<tr><td id=\"itemname\">{data.OrderName}</td><td id=\"itemcontext\">{data.OrderContext}</td></tr>");
            }

            body = body.Replace("#username", model.UserName);
            body = body.Replace("#items", sb.ToString());
            body = body.Replace("#timespan", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));

            return SendMailActive(new EmailModel(model.Email, title, body));
        }
    }
}
