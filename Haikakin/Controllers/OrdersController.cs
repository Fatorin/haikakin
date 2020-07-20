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
    [Route("api/v{version:apiVersion}/Orders")]
    //[Route("api/[controller]")]
    [ApiController]
    //[ApiExplorerSettings(GroupName = "HaikakinAPISpecOrder")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class OrdersController : ControllerBase
    {
        private IOrderRepository _orderRepo;
        private readonly IMapper _mapper;

        public OrdersController(IOrderRepository orderRepo, IMapper mapper)
        {
            _orderRepo = orderRepo;
            _mapper = mapper;
        }

        /// <summary>
        /// Get list on orders. For admin.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(List<OrderDto>))]
        [Authorize(Roles = "Admin")]
        public IActionResult GetOrders()
        {
            var objList = _orderRepo.GetOrders();

            var objDto = new List<OrderDto>();

            foreach (var obj in objList)
            {
                objDto.Add(_mapper.Map<OrderDto>(obj));
            }

            return Ok(objDto);
        }

        /// <summary>
        /// Get individual order
        /// </summary>
        /// <param name="orderId"> The id of the order</param>
        /// <returns></returns>
        [HttpGet("{orderId:int}", Name = "GetOrder")]
        [ProducesResponseType(200, Type = typeof(OrderDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        [Authorize(Roles = "VerifiedUser,Admin")]
        public IActionResult GetOrder(int orderId)
        {
            var obj = _orderRepo.GetOrder(orderId);

            if (obj == null)
            {
                return NotFound();
            }

            var objDto = _mapper.Map<OrderDto>(obj);

            return Ok(objDto);
        }

        [HttpPost]
        [ProducesResponseType(201, Type = typeof(OrderDto))]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "VerifiedUser,Admin")]
        public IActionResult CreateOrder([FromBody] OrderCreateDto orderDto)
        {
            if (orderDto == null)
            {
                return BadRequest(ModelState);
            }

            var orderObj = _mapper.Map<Order>(orderDto);

            if (!_orderRepo.CreateOrder(orderObj))
            {
                ModelState.AddModelError("", $"Something went wrong when save the data {orderObj.Id}");
                return StatusCode(500, ModelState);
            }

            return CreatedAtRoute("GetOrder", new { version = HttpContext.GetRequestedApiVersion().ToString(), orderId = orderObj.Id }, orderObj);
        }

        [HttpPatch("{orderId:int}", Name = "UpdateOrder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "User,Admin")]
        public IActionResult UpdateOrder(int orderId, [FromBody] OrderUpdateDto orderDto)
        {
            if (orderDto == null || orderId != orderDto.Id)
            {
                return BadRequest(ModelState);
            }

            var orderObj = _mapper.Map<Order>(orderDto);
            if (!_orderRepo.UpdateOrder(orderObj))
            {
                ModelState.AddModelError("", $"Something went wrong when updating the data {orderObj.Id}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        [HttpDelete("{orderId:int}", Name = "DeleteOrder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteOrder(int orderId)
        {
            if (!_orderRepo.OrderExists(orderId))
            {
                return NotFound();
            }

            var orderObj = _orderRepo.GetOrder(orderId);
            if (!_orderRepo.DeleteOrder(orderObj))
            {
                ModelState.AddModelError("", $"Something went wrong when updating the data {orderObj.Id}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        [HttpGet("[action]/{userId:int}")]
        [ProducesResponseType(200, Type = typeof(OrderDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        [Authorize(Roles = "VerifiedUser,Admin")]
        public IActionResult GetOrderInUser(int userId)
        {
            var objList = _orderRepo.GetOrdersInUser(userId);

            if (objList == null)
            {
                return NotFound();
            }

            var objDto = new List<OrderDto>();
            foreach (var obj in objList)
            {
                objDto.Add(_mapper.Map<OrderDto>(obj));
            }


            return Ok(objDto);
        }
    }
}
