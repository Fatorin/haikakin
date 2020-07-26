using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class TestModel
    {
        public string Title { get; set; }

        public DateTime Date { get; set; }

        public List<IFormFile> Photos { get; set; }
    }
}
