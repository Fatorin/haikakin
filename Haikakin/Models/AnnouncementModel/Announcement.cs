using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.AnnouncementModel
{
    public class Announcement
    {
        [Key]
        public int AnnouncementId { get; set; }
        [StringLength(100)]
        public string Title { get; set; }
        [StringLength(200)]
        public string ShortContext { get; set; }
        [StringLength(200)]
        public string FullContext { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public bool IsActive { get; set; }
    }
}
