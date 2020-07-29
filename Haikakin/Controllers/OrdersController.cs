using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Haikakin.Extension;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Haikakin.Models.Order;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/Orders")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class OrdersController : ControllerBase
    {
        private IOrderRepository _orderRepo;
        private IOrderInfoRepository _orderInfoRepo;
        private IProductRepository _productRepo;
        private IProductInfoRepository _productInfoRepo;
        private readonly IMapper _mapper;

        public OrdersController(IOrderRepository orderRepo, IOrderInfoRepository orderInfoRepo, IProductRepository productRepo, IProductInfoRepository productInfoRepo, IMapper mapper)
        {
            _orderRepo = orderRepo;
            _orderInfoRepo = orderInfoRepo;
            _productRepo = productRepo;
            _productInfoRepo = productInfoRepo;
            _mapper = mapper;
        }

        /// <summary>
        /// 獲得所有訂單，只有Admin可用
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
        /// 查詢指定訂單，User只可以查自己的，Admin不限制
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

        /// <summary>
        /// 建立訂單
        /// </summary>
        /// <param name="orderDtos"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(201, Type = typeof(OrderDto))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "User,Admin")]
        public IActionResult CreateOrder([FromBody] OrderCreateDto[] orderDtos)
        {
            //設定初值與使用者Token確認
            var price = 0.0;
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return BadRequest(new { message = "Bad token" });
            }
            var userId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
            //沒收到資料代表請求失敗
            if (orderDtos == null)
            {
                return BadRequest(ModelState);
            }
            //依序檢查商品剩餘數量並計算總價錢
            foreach (OrderCreateDto dto in orderDtos)
            {
                //檢查是否有該商品
                if (!_productRepo.ProductExists(dto.ProductId))
                {
                    //商品不存在
                    return BadRequest(new { message = "不存在的商品" });
                }
                //不能買
                var product = _productRepo.GetProduct(dto.ProductId);
                if (!product.CanBuy)
                {
                    return BadRequest(new { message = "已下架" });
                }
                //超過購買限制
                if (product.Limit != 0)
                {
                    return BadRequest(new { message = "超過數量購買限制" });
                }

                //商品數量錯誤
                if (dto.OrderCount == 0)
                {
                    return BadRequest(new { message = "購買數量為0" });
                }
                //數量超過庫存
                if (dto.OrderCount > product.Stock)
                {
                    return BadRequest(new { message = "庫存不足" });
                }

                price += (product.Price) * dto.OrderCount;
            }

            //產生訂單需求
            double exchange = ExchangeParse.GetExchange();
            //依序將商品加入訂單
            var order = new Order()
            {
                OrderCreateTime = DateTime.UtcNow,
                OrderPrice = price,
                OrderPay = OrderPayType.None,
                OrderStatus = OrderStatusType.NonPayment,
                OrderPaySerial = 0,
                ExchangeRate = exchange,
                UserId = userId
            };

            var orderId = _orderRepo.CreateOrder(order);
            //產生訂單物件
            foreach (OrderCreateDto dto in orderDtos)
            {
                OrderInfo orderInfo = new OrderInfo()
                {
                    ProductId = dto.ProductId,
                    OrderId = orderId,
                    Count = dto.OrderCount,
                    OrderTime = DateTime.UtcNow,
                };
                _orderInfoRepo.CreateOrderInfo(orderInfo);
            }

            return Ok();
        }

        /// <summary>
        /// 更新指定訂單，暫時還有問題
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="orderDto"></param>
        /// <returns></returns>
        [HttpPatch("{orderId:int}", Name = "FinishOrder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public IActionResult FinishOrder(int orderId, [FromBody] OrderUpdateDto orderDto)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new { message = "Bad token" });
            }

            if (orderDto == null || orderId != orderDto.OrderId)
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
                    return BadRequest(new { message = "這不是你的訂單" });
                }
            }

            if (_orderRepo.GetOrder(orderId).OrderStatus == OrderStatusType.AlreadyPaid)
            {
                return BadRequest(new { message = "訂單已結束無法更改" });
            }
            //檢查金流資訊

            var orderInfos = _orderInfoRepo.GetOrderInfosByOrderId(orderObj.OrderId);
            /*if (!_orderInfoRepo.UpdateOrderInfos(orderInfos))
            {
                return BadRequest(new { message = "訂單資料更新錯誤" });
            }*/
            //模擬完成結帳
            //var orderCheckStatus = OrderStatusType.Over;
            var productInfoStatus = ProductInfo.ProductStatusEnum.Used;

            foreach (OrderInfo orderInfo in orderInfos)
            {
                foreach (ProductInfo productInfo in orderInfo.ProductInfos)
                {
                    //訂單改成鎖定
                    productInfo.ProductStatus = productInfoStatus;
                    _productInfoRepo.UpdateProductInfo(productInfo);
                }
            }

            if (!_orderRepo.UpdateOrder(orderObj))
            {
                ModelState.AddModelError("", $"Something went wrong when updating the data {orderObj.OrderId}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        /// <summary>
        /// 取消訂單
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpPatch("CancelOrder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "User,Admin")]
        public IActionResult CancelOrder(int orderId)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new { message = "Bad token" });
            }

            var userId = identity.FindFirst(ClaimTypes.Name).Value;
            var role = identity.FindFirst(ClaimTypes.Role).Value;

            var orderObj = _orderRepo.GetOrder(orderId);
            if (orderObj == null)
            {
                return NotFound(new { message = "查無此訂單" });
            }

            if (role != "Admin")
            {
                if (orderObj.UserId != int.Parse(userId))
                {
                    return BadRequest(new { message = "這不是你的訂單" });
                }
            }

            if (orderObj.OrderStatus == OrderStatusType.Cancel)
            {
                return BadRequest(new { message = "訂單已取消" });
            }

            //更新庫存 解除已使用
            orderObj.OrderStatus = OrderStatusType.Cancel;
            orderObj.OrderLastUpdateTime = DateTime.UtcNow;
            _orderRepo.UpdateOrder(orderObj);
            foreach (OrderInfo orderInfo in orderObj.OrderInfos)
            {
                foreach (ProductInfo productInfo in orderInfo.ProductInfos)
                {
                    productInfo.OrderInfoId = 0;
                    productInfo.LastUpdateTime = DateTime.UtcNow;
                    productInfo.ProductStatus = ProductInfo.ProductStatusEnum.NotUse;
                    //更新庫存寫在UpdateProductInfo裡面
                    _productInfoRepo.UpdateProductInfo(productInfo);
                }
            }

            return Ok(orderObj);
        }

        /// <summary>
        /// 獲得當前用戶的所有訂單
        /// </summary>
        /// <returns></returns>
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
