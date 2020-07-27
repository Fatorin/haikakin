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

        public ProductsInfoController(IProductInfoRepository productInfoRepo)
        {
            _productInfoRepo = productInfoRepo;
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
            if (productInfo == null)
            {
                return BadRequest(new { message = "不存在的ID" });
            }
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
            var productInfo = _productInfoRepo.GetProductInfo(productInfoId);
            
            if (productInfo == null)
            {
                return BadRequest(new { message = "不存在的ID" });
            }

            productInfo.Serial = serial;

            if (!_productInfoRepo.UpdateProductInfo(productInfo))
            {
                return BadRequest(new { message = "更新失敗，系統錯誤" });
            }

            return NoContent();
        }
    }
}
