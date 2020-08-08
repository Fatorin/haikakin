using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.EmailVerityModel;

namespace Haikakin.Models.MailModel
{
    public class EmailAccount
    {
        public string UserId { get; private set; }
        public string UserName { get; private set; }
        public string UserEmail { get; private set; }

        public EmailVerityEnum EmailVerityAction { get; set; }

        public EmailAccount(string userId, string userName, string userEmail, EmailVerityEnum emailVerityAction)
        {
            UserId = userId;
            UserName = userName;
            UserEmail = userEmail;
            EmailVerityAction = emailVerityAction;
        }
    }
}
