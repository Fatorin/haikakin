using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AutoMapper;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using Haikakin.Extension;
using Haikakin.Extension.Services;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Models.NewebPay;
using Haikakin.Models.OrderModel;
using Haikakin.Models.OrderScheduler;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using static Haikakin.Models.OrderModel.Order;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/Orders")]
    [ApiController]
    public partial class OrdersController : ControllerBase
    {
        private IUserRepository _userRepo;
        private IOrderRepository _orderRepo;
        private IOrderInfoRepository _orderInfoRepo;
        private IProductRepository _productRepo;
        private IProductInfoRepository _productInfoRepo;
        private readonly IMapper _mapper;
        private AppSettings _appSettings;
        private readonly OrderJob _orderJob;
        private IScheduler _scheduler;
        private readonly ILogger<NewebPayBaseResp> _logger;

        public OrdersController(IUserRepository userRepo, IOrderRepository orderRepo, IOrderInfoRepository orderInfoRepo, IProductRepository productRepo, IProductInfoRepository productInfoRepo, IMapper mapper, IOptions<AppSettings> appSettings, OrderJob orderJob, IScheduler scheduler, ILogger<NewebPayBaseResp> logger)
        {
            _userRepo = userRepo;
            _orderRepo = orderRepo;
            _orderInfoRepo = orderInfoRepo;
            _productRepo = productRepo;
            _productInfoRepo = productInfoRepo;
            _mapper = mapper;
            _appSettings = appSettings.Value;
            _orderJob = orderJob;
            _scheduler = scheduler;
            _logger = logger;
        }

        /// <summary>
        /// 獲得所有訂單，只有Admin可用
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="orderStatus">NonPayment, AlreadyPaid, Over, Cancel</param>
        /// <returns></returns>
        [HttpGet("GetOrders")]
        [ProducesResponseType(200, Type = typeof(List<OrderDto>))]
        [Authorize(Roles = "Admin")]
        public IActionResult GetOrders(DateTime startTime, DateTime endTime, short? orderStatus)
        {
            var objList = _orderRepo.GetOrdersWithTimeRange(startTime, endTime, orderStatus);

            var objDto = new List<OrderDto>();

            foreach (var obj in objList)
            {
                objDto.Add(_mapper.Map<OrderDto>(obj));
            }

            return Ok(objDto);
        }

        /// <summary>
        /// 查詢指定訂單，User專用
        /// </summary>
        /// <param name="orderId"> The id of the order</param>
        /// <returns></returns>
        [HttpGet("{orderId:int}", Name = "GetOrder")]
        [ProducesResponseType(200, Type = typeof(OrderResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [Authorize(Roles = "User")]
        public IActionResult GetOrder(int orderId)
        {
            var obj = _orderRepo.GetOrder(orderId);

            if (obj == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "訂單不存在" });
            }

            HttpContextGetUserId(out bool result, out int userId);
            if (!result)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "使用者Token異常" });
            }

            //檢查用戶是否存在
            var user = _userRepo.GetUser(obj.UserId);
            if (user == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "用戶不存在" });
            }

            if (userId != obj.UserId)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "非使用者訂單" });
            }

            var orderInfoRespList = new List<OrderInfoResponse>();
            foreach (var orderInfo in obj.OrderInfos)
            {
                var productName = _productRepo.GetProduct(orderInfo.ProductId).ProductName;
                orderInfoRespList.Add(new OrderInfoResponse(productName, orderInfo.Count, null));
            }

            var orderRespModel = new OrderResponse()
            {
                OrderId = obj.OrderId,
                OrderCreateTime = obj.OrderCreateTime,
                OrderLastUpdateTime = obj.OrderLastUpdateTime,
                OrderStatus = obj.OrderStatus,
                OrderAmount = decimal.ToInt32(obj.OrderAmount),
                OrderPayWay = obj.OrderPayWay,
                OrderPaySerial = obj.OrderPaySerial,
                OrderCVSCode = obj.OrderCVSCode,
                OrderFee = obj.OrderFee,
                OrderPayLimitTime = obj.OrderPayLimitTime,
                OrderThirdPaySerial = obj.OrderThirdPaySerial,
                OrderInfos = orderInfoRespList,
                UserId = user.UserId,
                UserIPAddress = user.IPAddress,
                UserEmail = user.Email,
                UserName = user.Username
            };

            return Ok(orderRespModel);
        }

        /// <summary>
        /// 建立訂單
        /// </summary>
        /// <param name="orderDtos"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(NewebPayBase))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [Authorize(Roles = "User,Admin")]
        public IActionResult CreateOrder([FromBody] OrderCreateDto[] orderDtos)
        {
            if (!GlobalSetting.OrderSwitch)
            {
                return StatusCode(403, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統禁止下單" });
            }
            //設定初值與使用者Token確認
            decimal price = 0;
            HttpContextGetUserId(out bool result, out int userId);
            if (!result)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "使用者Token異常" });
            }
            //沒收到資料代表請求失敗
            if (orderDtos == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料錯誤" });
            }

            if (orderDtos.Length <= 0)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料錯誤" });
            }
            //檢查用戶是否存在
            var user = _userRepo.GetUser(userId);
            if (user == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "用戶不存在" });
            }
            //檢查是不是被BAN
            if (!user.EmailVerity || !user.PhoneNumberVerity)
            {
                return StatusCode(403, new ErrorPack { ErrorCode = 1000, ErrorMessage = "此用戶尚未認證" });
            }
            //檢查是不是被BAN
            if (user.CheckBan)
            {
                return StatusCode(403, new ErrorPack { ErrorCode = 1000, ErrorMessage = "此用戶已被黑名單" });
            }
            //檢查訂單未付款的項目，過多就擋住
            /*int notPayCount = _orderRepo.GetOrdersInUser(user.UserId).Where(o => o.OrderStatus == OrderStatusType.NotGetCVSCode).Count();
            if (notPayCount >= 3)
            {
                return StatusCode(403, new ErrorPack { ErrorCode = 1000, ErrorMessage = "過多訂單未付款" });
            }*/
            //依序檢查商品剩餘數量並計算總價錢
            foreach (OrderCreateDto dto in orderDtos)
            {
                //檢查是否有該商品
                if (!_productRepo.ProductExists(dto.ProductId))
                {
                    //商品不存在
                    return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的商品" });
                }
                //不能買
                var product = _productRepo.GetProduct(dto.ProductId);
                if (!product.CanBuy)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "商品已下架" });
                }
                //超過購買限制
                if (product.Limit != 0 && dto.OrderCount > product.Limit)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "超過最大購買限制" });
                }
                //商品數量錯誤
                if (dto.OrderCount <= 0)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "購買數量異常" });
                }
                //數量超過庫存
                if (dto.OrderCount > product.Stock)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "庫存不足" });
                }

                //原始價格
                price += product.Price * dto.OrderCount;
                //代購費用抽成
                price += product.Price * dto.OrderCount * product.AgentFeePercent / 100;
            }

            if (price <= 0)
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "金額計算異常，下單失敗" });
            }

            //用爬蟲抓匯率
            decimal exchange = ExchangeParseService.GetExchange();
            //產生訂單物件
            var orderObj = new Order()
            {
                OrderCreateTime = DateTime.UtcNow,
                OrderAmount = price,
                OrderPayWay = OrderPayWayEnum.CVSBarCode,
                OrderStatus = OrderStatusType.NotGetCVSCode,
                OrderLastUpdateTime = DateTime.UtcNow,
                OrderFee = $"{GlobalSetting.OrderFee}",
                Exchange = exchange,
                UserId = userId,
            };
            //DB建立訂單物件(不含訂單編號與檢查碼)
            var orderCreatedObj = _orderRepo.CreateOrder(orderObj);
            //產生訂單詳細資訊
            foreach (OrderCreateDto dto in orderDtos)
            {
                OrderInfo orderInfo = new OrderInfo()
                {
                    ProductId = dto.ProductId,
                    OrderId = orderCreatedObj.OrderId,
                    Count = dto.OrderCount,
                    OrderTime = DateTime.UtcNow,
                };
                _orderInfoRepo.CreateOrderInfo(orderInfo);
            }

            //定時器開啟，一個小時內沒取號視同棄單
            _orderJob.StartJob(_scheduler, orderCreatedObj.OrderId);

            return StatusCode(201, orderCreatedObj.OrderId);
        }

        /// <summary>
        /// 取消訂單，只有管理員可使用
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpPatch("CancelOrder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [Authorize(Roles = "Admin")]
        public IActionResult CancelOrder(int orderId)
        {
            var orderObj = _orderRepo.GetOrder(orderId);
            if (orderObj == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "查無此訂單" });
            }

            if (orderObj.OrderStatus == OrderStatusType.Over)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "訂單已結束" });
            }

            if (orderObj.OrderStatus == OrderStatusType.Cancel)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "訂單已取消" });
            }

            //更新庫存 解除已使用
            orderObj.OrderStatus = OrderStatusType.Cancel;
            orderObj.OrderLastUpdateTime = DateTime.UtcNow;
            _orderRepo.UpdateOrder(orderObj);
            foreach (OrderInfo orderInfo in orderObj.OrderInfos)
            {
                var productInfos = _productInfoRepo
                    .GetProductInfos()
                    .Where(o => o.OrderInfoId == orderInfo.OrderInfoId)
                    .Where(o => o.ProductId == orderInfo.ProductId)
                    .ToList();

                foreach (ProductInfo productInfo in productInfos)
                {
                    productInfo.OrderInfoId = null;
                    productInfo.LastUpdateTime = DateTime.UtcNow;
                    productInfo.ProductStatus = ProductInfo.ProductStatusEnum.NotUse;
                    _productInfoRepo.UpdateProductInfo(productInfo);
                }

                //更新庫存
                _productRepo.UpdateProduct(_productRepo.GetProduct(orderInfo.ProductId));
            }

            //計算棄單次數，如果超過就吃BAN
            var user = _userRepo.GetUser(orderObj.UserId);
            user.CancelTimes++;
            if (user.CancelTimes >= 3)
            {
                user.CheckBan = true;
            }
            _userRepo.UpdateUser(user);

            return Ok();
        }

        /// <summary>
        /// 獲得當前用戶的所有訂單
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetOrderInUser")]
        [ProducesResponseType(200, Type = typeof(List<OrderDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [Authorize(Roles = "User,Admin")]
        public IActionResult GetOrderInUser()
        {
            HttpContextGetUserId(out bool result, out int userId);
            if (!result)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "使用者Token異常" });
            }

            var objList = _orderRepo.GetOrdersInUser(userId);

            if (objList == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "查無此使用者" });
            }

            var objDto = new List<OrderDto>();
            foreach (var obj in objList)
            {
                objDto.Add(_mapper.Map<OrderDto>(obj));
            }

            return Ok(objDto);
        }

        /// <summary>
        /// 獲得當前用戶的所有訂單
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetOrderInUserForAdmin")]
        [ProducesResponseType(200, Type = typeof(List<OrderDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [Authorize(Roles = "Admin")]
        public IActionResult GetOrderInUserForAdmin(int userId)
        {
            var objList = _orderRepo.GetOrdersInUser(userId);

            if (objList == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "查無此使用者" });
            }

            var objDto = new List<OrderDto>();
            foreach (var obj in objList)
            {
                objDto.Add(_mapper.Map<OrderDto>(obj));
            }

            return Ok(objDto);
        }

        /// <summary>
        /// 開啟或關閉下單功能
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        [HttpGet("SwitchOrder")]
        [ProducesResponseType(200, Type = typeof(List<OrderDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [Authorize(Roles = "Admin")]
        public IActionResult SwitchOrder(bool trigger)
        {
            GlobalSetting.OrderSwitch = trigger;
            _logger.LogInformation($"下單功能已調整為:{trigger}");
            return Ok();
        }

        private void HttpContextGetUserId(out bool result, out int userId)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                result = false;
                userId = 0;
            }
            else
            {
                result = true;
                userId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
            }
        }
    }
}
