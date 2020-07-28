using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.User;

namespace Haikakin.Models
{
    public class AuthenticationThirdModel
    {
        [Required]
        public string TokenId { get; set; }
        [Required]
        public LoginTypeEnum LoginType { get; set; }
    }
}
