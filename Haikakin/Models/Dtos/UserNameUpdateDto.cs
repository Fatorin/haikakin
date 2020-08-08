using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.User;

namespace Haikakin.Models.Dtos
{
    public class UserNameUpdateDto
    {
        public int UserId { get; set; }
        [Required]
        public string Username { get; set; }
    }

}
