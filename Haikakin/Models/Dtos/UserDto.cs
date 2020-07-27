using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.User;

namespace Haikakin.Models.Dtos
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public bool EmailVerity { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberVerity { get; set; }
        public string IPAddress { get; set; }
        public string Role { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime CreateTime { get; set; }

        public LoginTypeEnum LoginType { get; set; }

        public bool CheckBan { get; set; }
    }

}
