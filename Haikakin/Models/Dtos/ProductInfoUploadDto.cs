using Haikakin.Models.UploadModel;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.Dtos
{
    public class ProductInfoUploadDto
    {
        public int ProductId { get; set; }
        public decimal PrimeCost { get; set; }
        [Required(ErrorMessage = "Error format or size too big.")]
        [DataType(DataType.Upload)]
        [MaxFileSize(5 * 1024 * 1024)]
        [AllowedExtensions(new string[] { ".txt" })]
        public IFormFile SerialFile { get; set; }
    }
}
