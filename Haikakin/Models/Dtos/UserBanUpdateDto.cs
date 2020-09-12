using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.Dtos
{
    public class UserBanUpdateDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public bool CheckBan { get; set; }
    }
}
