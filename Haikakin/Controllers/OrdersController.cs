using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using AutoMapper;
using Haikakin.Extension;
using Haikakin.Extension.ECPay;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Models.ECPayModel;
using Haikakin.Models.MailModel;
using Haikakin.Models.OrderModel;
using Haikakin.Models.OrderScheduler;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using RestSharp;
using RestSharp.Authenticators;
using static Haikakin.Models.Order;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/Orders")]
    [ApiController]
    public class OrdersController : ControllerBase
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

        public OrdersController(IUserRepository userRepo, IOrderRepository orderRepo, IOrderInfoRepository orderInfoRepo, IProductRepository productRepo, IProductInfoRepository productInfoRepo, IMapper mapper, IOptions<AppSettings> appSettings, OrderJob orderJob, IScheduler scheduler)
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
        [ProducesResponseType(200, Type = typeof(OrderResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [Authorize(Roles = "User,Admin")]
        public IActionResult GetOrder(int orderId)
        {
            var obj = _orderRepo.GetOrder(orderId);

            if (obj == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "訂單不存在" });
            }

            var orderInfoRespList = new List<OrderInfoResponse>();
            foreach (var orderInfo in obj.OrderInfos)
            {
                var productName = _productRepo.GetProduct(orderInfo.ProductId).ProductName;
                orderInfoRespList.Add(new OrderInfoResponse(productName, orderInfo.Count));
            }

            var orderRespModel = new OrderResponse()
            {
                OrderId = obj.OrderId,
                OrderCreateTime = obj.OrderCreateTime,
                OrderLastUpdateTime = obj.OrderLastUpdateTime,
                OrderStatus = obj.OrderStatus,
                OrderPayWay = obj.OrderPayWay,
                OrderPaySerial = obj.OrderPaySerial,
                OrderPrice = decimal.ToInt32(obj.OrderPrice),
                OrderInfos = orderInfoRespList
            };

            return Ok(orderRespModel);
        }

        /// <summary>
        /// 建立訂單
        /// </summary>
        /// <param name="orderDtos"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ECPaymentModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [Authorize(Roles = "User,Admin")]
        public IActionResult CreateOrder([FromBody] OrderCreateDto[] orderDtos)
        {
            //設定初值與使用者Token確認
            decimal price = 0;
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "使用者Token異常" });
            }
            var userId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
            //沒收到資料代表請求失敗
            if (orderDtos == null)
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
            //依序檢查商品剩餘數量並計算總價錢
            //紀錄商品名稱
            var itemsNameList = new List<string>();
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
                if (dto.OrderCount == 0)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "購買數量異常" });
                }
                //數量超過庫存
                if (dto.OrderCount > product.Stock)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "庫存不足" });
                }

                price += product.Price * dto.OrderCount;
                itemsNameList.Add($"{product.ProductName} x {dto.OrderCount}");
            }
            //用爬蟲抓匯率
            decimal exchange = ExchangeParse.GetExchange();
            //產生訂單物件
            var orderObj = new Order()
            {
                OrderCreateTime = DateTime.UtcNow,
                OrderPrice = price,
                OrderPayWay = OrderPayWayEnum.None,
                OrderStatus = OrderStatusType.NonPayment,
                OrderLastUpdateTime = DateTime.UtcNow,
                OrderECPayLimitTime = DateTime.UtcNow.AddMinutes(15),
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

            //產生綠界訂單
            var ecPayNo = $"ecPay{orderCreatedObj.OrderId}";
            ECPaymentModel model = new ECPaymentModel(ecPayNo, decimal.ToInt32(orderCreatedObj.OrderPrice), "數位商品訂單", itemsNameList, "數位商品");
            var postModel = new ECPaymentPostModel(model);
            model.CheckMacValue = ECPayCheckValue.BuildCheckMacValue(postModel.PostValue, _appSettings.ECPayHashKey, _appSettings.ECPayHashIV, 1);
            //更新訂單的訂單編號與檢查碼
            orderCreatedObj.OrderPaySerial = ecPayNo;
            orderCreatedObj.OrderCheckCode = model.CheckMacValue;
            _orderRepo.UpdateOrder(orderCreatedObj);
            //定時器開啟，一個小時內沒繳完費自動取消
            _orderJob.StartJob(_scheduler, orderObj.OrderId);

            return StatusCode(201, model);
        }

        /// <summary>
        /// 結束指定訂單，用於第三方的回傳，所以不限定使用者
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPatch("FinishOrder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [AllowAnonymous]
        public IActionResult FinishOrder([FromBody] ECPaymentResponseModel ecPayModel)
        {
            if (ecPayModel == null)
            {
                Console.WriteLine("0|資料請求異常");
                return BadRequest("0|資料請求異常");
            }
            //依照特店交易編號回傳資料
            var order = _orderRepo.GetOrdersInPaySerial(ecPayModel.MerchantTradeNo);

            if (order == null)
            {
                Console.WriteLine("0|查無此訂單");
                return BadRequest("0|查無此訂單");
            }

            if (order.OrderStatus == OrderStatusType.Over)
            {
                Console.WriteLine("0|資料請求異常");
                return BadRequest("0|訂單已結束");
            }

            if (order.OrderStatus == OrderStatusType.Cancel)
            {
                Console.WriteLine("0|特店訂單已取消");
                return BadRequest("0|特店訂單已取消");
            }

            //檢查金流資訊
            if (ecPayModel.CheckMacValue != order.OrderCheckCode)
            {
                Console.WriteLine("0|檢查碼錯誤");
                return BadRequest("0|檢查碼錯誤");
            }
            //此為模擬付款，會直接跳過 先暫時拿掉用於測試
            /*if (ecPayModel.SimulatePaid == 1)
            {
                return Ok("1|此為模擬付款");
            }*/

            if (ecPayModel.TradeAmt != order.OrderPrice)
            {
                Console.WriteLine("0|金額不相符");
                return BadRequest("0|金額不相符");
            }

            if (ecPayModel.RtnCode != 1)
            {
                Console.WriteLine("0|金額不相符");
                return BadRequest("0|付款失敗，不進行交易");
            }
            //如果金流資訊錯誤則回傳失敗            

            //獲得對應訂單並修改
            var user = _userRepo.GetUser(order.UserId);

            //orderInfo不用更新
            var emailOrderInfoList = new List<EmailOrderInfo>();
            foreach (OrderInfo orderInfo in order.OrderInfos)
            {
                //商品名稱與數量
                var emailInfo = new EmailOrderInfo();
                var productName = _productRepo.GetProduct(orderInfo.ProductId).ProductName;
                var orderCount = orderInfo.Count;

                emailInfo.OrderName = $"{productName} x{orderCount}";

                var orderContext = new StringBuilder();

                var productInfos = _productInfoRepo
                    .GetProductInfos()
                    .Where(o => o.OrderInfoId == orderInfo.OrderInfoId)
                    .Where(o => o.ProductId == orderInfo.ProductId)
                    .ToList();

                foreach (ProductInfo productInfo in productInfos)
                {
                    //訂單改成鎖定
                    productInfo.ProductStatus = ProductInfo.ProductStatusEnum.Used;
                    if (!_productInfoRepo.UpdateProductInfo(productInfo))
                    {
                        //系統更新資料異常
                        Console.WriteLine("0|特店系統異常_ProductInfo更新異常");
                        return BadRequest("0|特店系統異常");
                    };

                    orderContext.Append($"{productInfo.Serial}<br>");
                }

                emailInfo.OrderContext = orderContext.ToString();
                emailOrderInfoList.Add(emailInfo);
            }

            order.OrderStatus = OrderStatusType.Over;
            order.OrderLastUpdateTime = DateTime.UtcNow;
            //要將綠界編號儲存
            order.OrderECPaySerial = ecPayModel.TradeNo;
            if (!_orderRepo.UpdateOrder(order))
            {
                //$"更新資料錯誤，訂單編號{order.OrderId}"
                Console.WriteLine("0|特店系統異常_Order更新異常");
                return BadRequest("0|特店系統異常");
            }

            //沒問題就發送序號
            SendMailService service = new SendMailService(_appSettings.MailgunAPIKey);
            EmailOrderFinish mailModel = new EmailOrderFinish { UserName = user.Username, Email = user.Email, OrderId = $"{order.OrderId}", OrderItemList = emailOrderInfoList };
            if (!service.OrderFinishMailBuild(mailModel))
            {
                //信箱系統掛掉
                Console.WriteLine("0|特店系統異常_信箱");
                return BadRequest("0|特店系統異常");
            };

            return Ok("1|OK");
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
                    //更新庫存寫在UpdateProductInfo裡面
                    _productInfoRepo.UpdateProductInfo(productInfo);
                }
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
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "使用者身份異常" });
            }

            var userId = identity.FindFirst(ClaimTypes.Name).Value;

            var objList = _orderRepo.GetOrdersInUser(int.Parse(userId));

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
    }
}
