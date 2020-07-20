using System;
using System.Collections.Generic;
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
        /// Get list on products.
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
        /// Get individual product
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
                return NotFound();
            }

            var objDto = _mapper.Map<ProductDto>(obj);

            return Ok(objDto);
        }

        [HttpPost]
        [ProducesResponseType(201, Type = typeof(ProductDto))]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProduct([FromBody] ProductDto productDto)
        {
            if (productDto == null)
            {
                return BadRequest(ModelState);
            }

            var productObj = _mapper.Map<Product>(productDto);

            if (!_productRepo.CreateProduct(productObj))
            {
                ModelState.AddModelError("", $"Something went wrong when save the data {productObj.Id}");
                return StatusCode(500, ModelState);
            }

            return CreatedAtRoute("GetProduct", new { version = HttpContext.GetRequestedApiVersion().ToString(), productId = productObj.Id }, productObj);
        }

        [HttpPatch("{productId:int}", Name = "UpdateProduct")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateProduct(int productId, [FromBody] ProductDto productDto)
        {
            if (productDto == null || productId != productDto.Id)
            {
                return BadRequest(ModelState);
            }

            var productObj = _mapper.Map<Product>(productDto);
            if (!_productRepo.UpdateProduct(productObj))
            {
                ModelState.AddModelError("", $"Something went wrong when updating the data {productObj.Id}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        [HttpDelete("{productId:int}", Name = "DeleteProduct")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteProduct(int productId)
        {
            if (!_productRepo.ProductExists(productId))
            {
                return NotFound();
            }

            var productObj = _productRepo.GetProduct(productId);
            if (!_productRepo.DeleteProduct(productObj))
            {
                ModelState.AddModelError("", $"Something went wrong when updating the data {productObj.Id}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }
    }
}
