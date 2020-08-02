using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.MailModel
{
    public class EmailModel
    {
        public string Email { get; set; }
        public string EmailTitle { get; set; }
        public string EmailBody { get; set; }

        public EmailModel(string email, string emailTitle, string emailBody)
        {
            Email = email;
            EmailTitle = emailTitle;
            EmailBody = emailBody;
        }
    }
}
