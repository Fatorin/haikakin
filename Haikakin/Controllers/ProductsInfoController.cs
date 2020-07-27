using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/Products")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class ProductsInfoController : ControllerBase
    {
        private IProductInfoRepository _productInfoRepo;
        private readonly IMapper _mapper;

        public ProductsInfoController(IProductInfoRepository productInfoRepo, IMapper mapper)
        {
            _productInfoRepo = productInfoRepo;
            _mapper = mapper;
        }

        [HttpGet("GetProductInfos")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetProductInfos()
        {
            var productInfoList = _productInfoRepo.GetProductInfos().ToList();
            return Ok(productInfoList);
        }

        [HttpPost("GetProductInfo")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetProductInfo(int productInfoId)
        {
            var productInfo = _productInfoRepo.GetProductInfo(productInfoId);
            return Ok();
        }

        [HttpPost("CreateProductInfo")]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProductInfo([FromForm] ProductDto productDto)
        {
            return Ok();
        }

        [HttpPatch("UpdateProductInfo")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateProductInfo(int productInfoId, string serial)
        {
            return NoContent();
        }
    }
}
