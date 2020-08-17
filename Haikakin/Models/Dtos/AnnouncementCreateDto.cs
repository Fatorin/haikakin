using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.Dtos
{
    public class AnnouncementCreateDto
    {
        [StringLength(100)]
        public string Title { get; set; }
        [StringLength(200)]
        public string ShortContext { get; set; }
        [StringLength(200)]
        public string FullContext { get; set; }
        public string ImageUrl { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public bool IsActive { get; set; }
    }
}
