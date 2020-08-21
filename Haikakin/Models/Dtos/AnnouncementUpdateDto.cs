using Haikakin.Models.UploadModel;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.Dtos
{
    public class AnnouncementUpdateDto
    {
        [Required]
        public int AnnouncementId { get; set; }
        [StringLength(100)]
        public string Title { get; set; }
        [StringLength(200)]
        public string ShortContext { get; set; }
        [StringLength(200)]
        public string FullContext { get; set; }
        [DataType(DataType.Upload)]
        [MaxFileSize(5 * 1024 * 1024)]
        [AllowedExtensions(new string[] { ".jpg", ".png" })]
        public IFormFile Image { get; set; }
        public bool IsActive { get; set; }
    }
}
