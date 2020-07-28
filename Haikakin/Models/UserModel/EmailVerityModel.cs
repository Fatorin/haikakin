using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class EmailVerityModel
    {
        public int userId { get; set; }
        public string userEmail { get; set; }
        public enum EmailVerityAction { EmailVerity, EmailModify }

        public EmailVerityAction emailVerityAction { get; set; }
    }
}
