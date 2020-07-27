using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Required]
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public bool EmailVerity { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberVerity { get; set; }
        public string IPAddress { get; set; }
        public string Role { get; set; }
        [NotMapped]
        public string Token { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime CreateTime { get; set; }
        public enum LoginTypeEnum { Normal, Facebook, Google }

        public LoginTypeEnum LoginType { get; set; }

        public bool CheckBan { get; set; }
    }

}
