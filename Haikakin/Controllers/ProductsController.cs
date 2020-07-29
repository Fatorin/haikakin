using System.Collections.Generic;
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
        [HttpGet("GetProducts")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductDto>))]
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
        [HttpGet("GetProduct")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        public IActionResult GetProduct(int productId)
        {
            var obj = _productRepo.GetProduct(productId);

            if (obj == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的商品" });
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
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ProductDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProduct(ProductUpsertDto productDto)
        {
            if (productDto == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的商品" });
            }

            var productObj = _mapper.Map<Product>(productDto);

            if (!_productRepo.CreateProduct(productObj))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統異常" });
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
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateProduct([FromBody] ProductUpsertDto productDto)
        {
            if (productDto == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求錯誤" });
            }

            var productObj = _mapper.Map<Product>(productDto);

            if (!_productRepo.ProductExists(productObj.ProductId))
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的商品" });
            }

            if (!_productRepo.UpdateProduct(productObj))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = $"資料更新錯誤:{productObj.ProductId}" });
            }

            return NoContent();
        }
    }
}
