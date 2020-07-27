using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class ProductInfoFile
    {
        public int ProductId { get; set; }
        public List<IFormFile> FormFiles { get; set; }
    }
}
