using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.Dtos
{
    public class UserPasswordUpdateDto
    {
        public int UserId { get; set; }
        [Required]
        public string UserOldPassword { get; set; }
        [Required]
        public string UserPassword { get; set; }
        [Required]
        public string UserReCheckPassword { get; set; }
    }
}