using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class EmailVerityModel
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; }
        public enum EmailVerityEnum { EmailVerity, EmailModify }

        public EmailVerityEnum EmailVerityAction { get; set; }
    }
}
