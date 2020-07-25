using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        [Authorize(Roles = "User,Admin")]
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
        [Authorize(Roles = "User,Admin")]
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
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new { message = "Bad token" });
            }

            if (orderDto == null || orderId != orderDto.Id)
            {
                return BadRequest(ModelState);
            }
            //對應到相應的資料
            var orderObj = _mapper.Map<Order>(orderDto);

            //如果有不是管理者則自己身ID為主，此時傳值無效
            var userId = identity.FindFirst(ClaimTypes.Name).Value;
            var role = identity.FindFirst(ClaimTypes.Role).Value;
            if (role != "Admin")
            {
                if (orderObj.UserId != int.Parse(userId))
                {
                    return BadRequest(new { message = "Not current user." });
                }
            }

            if (!_orderRepo.UpdateOrder(orderObj))
            {
                ModelState.AddModelError("", $"Something went wrong when updating the data {orderObj.Id}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        [HttpGet("GetOrderInUser")]
        [ProducesResponseType(200, Type = typeof(OrderDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        [Authorize(Roles = "User,Admin")]
        public IActionResult GetOrderInUser()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new { message = "Bad token" });
            }

            var userId = identity.FindFirst(ClaimTypes.Name).Value;

            var objList = _orderRepo.GetOrdersInUser(int.Parse(userId));

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
