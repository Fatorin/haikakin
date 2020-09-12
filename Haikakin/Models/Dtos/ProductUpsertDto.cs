using Haikakin.Models.UploadModel;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.Dtos
{
    public class ProductUpsertDto
    {
        public int ProductId { get; set; }
        [Required]
        public string ProductName { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public bool CanBuy { get; set; }

        [DataType(DataType.Upload)]
        [MaxFileSize(5 * 1024 * 1024)]
        [AllowedExtensions(new string[] { ".jpg", ".png" })]
        public IFormFile Image { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string ExtraDescription { get; set; }
        [Required]
        public int Limit { get; set; }
        [Required]
        public int ItemType { get; set; }
        [Required]
        public int ItemOrder { get; set; }
    }
}
