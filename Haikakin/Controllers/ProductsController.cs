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
using Twilio.TwiML.Messaging;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/Products")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class ProductsController : ControllerBase
    {
        private IProductRepository _productRepo;
        private readonly IMapper _mapper;

        public ProductsController(IProductRepository productRepo, IMapper mapper)
        {
            _productRepo = productRepo;
            _mapper = mapper;
        }

        /// <summary>
        /// 獲得全部商品資訊
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(List<ProductDto>))]
        public IActionResult GetProducts()
        {
            var objList = _productRepo.GetProducts();

            var objDto = new List<ProductDto>();

            foreach (var obj in objList)
            {
                objDto.Add(_mapper.Map<ProductDto>(obj));
            }

            return Ok(objDto);
        }

        /// <summary>
        /// 獲得指定商品資訊
        /// </summary>
        /// <param name="productId"> The id of the product</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("{productId:int}", Name = "GetProduct")]
        [ProducesResponseType(200, Type = typeof(ProductDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult GetProduct(int productId)
        {
            var obj = _productRepo.GetProduct(productId);

            if (obj == null)
            {
                return NotFound(new { message = "不存在的商品" });
            }

            var objDto = _mapper.Map<ProductDto>(obj);

            return Ok(objDto);
        }

        /// <summary>
        /// 建立指定商品，管理員才可使用
        /// </summary>
        /// <param name="productDto"></param>
        /// <returns></returns>
        [HttpPost("CreateProduct")]
        [ProducesResponseType(201, Type = typeof(ProductDto))]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProduct(ProductUpsertDto productDto)
        {
            if (productDto == null)
            {
                return BadRequest(new { message = "請求錯誤" });
            }

            var productObj = _mapper.Map<Product>(productDto);

            if (!_productRepo.CreateProduct(productObj))
            {
                ModelState.AddModelError("", $"Something went wrong when save the data {productObj.ProductId}");
                return StatusCode(500, ModelState);
            }

            return CreatedAtRoute("GetProduct", new { version = HttpContext.GetRequestedApiVersion().ToString(), productId = productObj.ProductId }, productObj);
        }

        /// <summary>
        /// 更新指定商品，管理員才可使用
        /// </summary>
        /// <param name="productDto"></param>
        /// <returns></returns>
        [HttpPatch("UpdateProduct")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateProduct([FromBody] ProductUpsertDto productDto)
        {
            if (productDto == null)
            {
                return BadRequest(new { message = "請求錯誤" });
            }

            var productObj = _mapper.Map<Product>(productDto);

            if (!_productRepo.ProductExists(productObj.ProductId))
            {
                return NotFound(new { message = "不存在的商品" });
            }

            if (!_productRepo.UpdateProduct(productObj))
            {
                ModelState.AddModelError("", $"商品名稱錯誤 {productObj.ProductId}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }
    }
}
