using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.Dtos
{
    public class UserEmailUpdateDto
    {
        public int UserId { get; set; }
        [Required]
        public string UserEmail { get; set; }
    }
}
