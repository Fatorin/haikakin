using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;
using AutoMapper;
using Haikakin.Extension;
using Haikakin.Extension.NewebPayUtil;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Models.MailModel;
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
        /// 查詢指定訂單，User專用
        /// </summary>
        /// <param name="orderId"> The id of the order</param>
        /// <returns></returns>
        [HttpGet("{orderId:int}", Name = "GetOrder")]
        [ProducesResponseType(200, Type = typeof(OrderResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [Authorize(Roles = "User")]
        public IActionResult GetOrder(int orderId)
        {
            var obj = _orderRepo.GetOrder(orderId);

            if (obj == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "訂單不存在" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "使用者Token異常" });
            }

            var userId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);

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
                OrderPayWay = obj.OrderPayWay,
                OrderPaySerial = obj.OrderPaySerial,
                OrderAmount = decimal.ToInt32(obj.OrderAmount),
                OrderInfos = orderInfoRespList,
                UserId = user.UserId,
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
                if (dto.OrderCount <= 0)
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
                OrderAmount = price,
                OrderPayWay = OrderPayWayEnum.CVSBarCode,
                OrderStatus = OrderStatusType.NonPayment,
                OrderLastUpdateTime = DateTime.UtcNow,
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

            //產生藍新訂單
            var newebPayNo = $"N{DateTime.Now.ToString("yyyyMMddHHmm")}{orderCreatedObj.OrderId.ToString().Substring(4)}";
            var items = new StringBuilder();
            foreach (var item in itemsNameList)
            {
                items.AppendLine(item);
            }
            var model = CreateNewbPayData(newebPayNo, items.ToString(), decimal.ToInt32(price), user.Email, "CVS");
            //更新訂單的訂單編號
            orderCreatedObj.OrderPaySerial = newebPayNo;
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
        [HttpPost("FinishOrder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
        [AllowAnonymous]
        public IActionResult FinishOrder([FromForm] NewebPayBaseResp newebPayModel)
        {
            if (newebPayModel == null)
            {
                _logger.LogInformation("資料請求異常");
                return BadRequest("資料請求異常");
            }

            if (newebPayModel.Status != "SUCCESS")
            {
                _logger.LogInformation($"交易失敗，錯誤代碼：{newebPayModel.Status}");
            }

            //檢查資料
            if (newebPayModel.MerchantID != _appSettings.NewebPayMerchantID)
            {
                _logger.LogInformation("非本商店訂單");
                return BadRequest("非本商店訂單");
            }

            if (newebPayModel.TradeSha != CryptoUtil.EncryptSHA256($"HashKey={_appSettings.NewebPayHashKey}&{newebPayModel.TradeInfo}&HashIV={_appSettings.NewebPayHashIV}"))
            {
                _logger.LogInformation("非本商店訂單");
                return BadRequest("非本商店訂單");
            }

            var decryptTradeInfo = CryptoUtil.DecryptAESHex(newebPayModel.TradeInfo, _appSettings.NewebPayHashKey, _appSettings.NewebPayHashIV);

            // 取得回傳參數(ex:key1=value1&key2=value2),儲存為NameValueCollection
            NameValueCollection decryptTradeCollection = HttpUtility.ParseQueryString(decryptTradeInfo);
            NewebPayResponse convertModel = LambdaUtil.DictionaryToObject<NewebPayResponse>(decryptTradeCollection.AllKeys.ToDictionary(k => k, k => decryptTradeCollection[k]));

            //依照特店交易編號回傳資料
            var order = _orderRepo.GetOrdersInPaySerial(convertModel.MerchantOrderNo);

            if (order == null)
            {
                _logger.LogInformation($"查無此訂單:{convertModel.MerchantOrderNo}");
                return BadRequest("查無此訂單");
            }

            if (order.OrderStatus == OrderStatusType.Over)
            {
                _logger.LogInformation("資料請求異常");
                return BadRequest("訂單已結束");
            }

            if (order.OrderStatus == OrderStatusType.Cancel)
            {
                _logger.LogInformation("特店訂單已取消");
                return BadRequest("特店訂單已取消");
            }

            if (convertModel.Amt != order.OrderAmount)
            {
                _logger.LogInformation("金額不相符");
                _logger.LogInformation($"convertModel.Amt={convertModel.Amt},order.OrderPrice={order.OrderAmount}");
                return BadRequest("金額不相符");
            }

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
                        _logger.LogInformation("特店系統異常_ProductInfo更新異常");
                        return BadRequest("特店系統異常");
                    };
                    productInfo.Serial = CryptoUtil.DecryptAESHex(productInfo.Serial, _appSettings.SerialHashKey, _appSettings.SerialHashIV);
                    orderContext.Append($"{productInfo.Serial}<br>");
                }

                emailInfo.OrderContext = orderContext.ToString();
                emailOrderInfoList.Add(emailInfo);
            }

            order.OrderStatus = OrderStatusType.Over;
            order.OrderLastUpdateTime = DateTime.UtcNow;
            //要將綠界編號儲存
            order.OrderThirdPaySerial = convertModel.TradeNo;
            if (!_orderRepo.UpdateOrder(order))
            {
                //$"更新資料錯誤，訂單編號{order.OrderId}"
                _logger.LogInformation("特店系統異常_Order更新異常");
                return BadRequest("特店系統異常");
            }

            //沒問題就發送序號
            SendMailService service = new SendMailService(_appSettings.MailgunAPIKey);
            EmailOrderFinish mailModel = new EmailOrderFinish { UserName = user.Username, Email = user.Email, OrderId = $"{order.OrderId}", OrderItemList = emailOrderInfoList };
            if (!service.OrderFinishMailBuild(mailModel))
            {
                //信箱系統掛掉
                _logger.LogInformation("序號發送異常");
                return BadRequest("特店系統異常");
            };

            return NoContent();
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

        private NewebPayBase CreateNewbPayData(string ordernumber, string orderItems, int amount, string userEmail, string payType)
        {
            // 目前時間轉換 +08:00, 防止傳入時間或Server時間時區不同造成錯誤
            DateTimeOffset taipeiStandardTimeOffset = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0));
            // 預設參數
            var version = "1.5";
            var returnURL = "https://www.haikakin.com/account/order";
            var notifyURL = "https://www.haikakin.com/api/v1/Orders/FinishOrder";

            NewebPayRequest newebPayRequest = new NewebPayRequest()
            {
                // * 商店代號
                MerchantID = _appSettings.NewebPayMerchantID,
                // * 回傳格式
                RespondType = "String",
                // * TimeStamp
                TimeStamp = taipeiStandardTimeOffset.ToUnixTimeSeconds().ToString(),
                // * 串接程式版本
                Version = version,
                // * 商店訂單編號
                MerchantOrderNo = ordernumber,
                // * 訂單金額
                Amt = amount,
                // * 商品資訊
                ItemDesc = orderItems,
                // 繳費有效期限(適用於非即時交易)
                ExpireDate = null,
                // 支付完成 返回商店網址
                ReturnURL = null,
                // 支付通知網址
                NotifyURL = notifyURL,
                // 商店取號網址
                CustomerURL = null,
                // 支付取消 返回商店網址
                ClientBackURL = returnURL,
                // * 付款人電子信箱
                Email = userEmail,
                // 付款人電子信箱 是否開放修改(1=可修改 0=不可修改)
                EmailModify = 0,
                // 商店備註
                OrderComment = null,
                // 信用卡 一次付清啟用(1=啟用、0或者未有此參數=不啟用)
                CREDIT = null,
                // WEBATM啟用(1=啟用、0或者未有此參數，即代表不開啟)
                WEBATM = null,
                // ATM 轉帳啟用(1=啟用、0或者未有此參數，即代表不開啟)
                VACC = null,
                // 超商代碼繳費啟用(1=啟用、0或者未有此參數，即代表不開啟)(當該筆訂單金額小於 30 元或超過 2 萬元時，即使此參數設定為啟用，MPG 付款頁面仍不會顯示此支付方式選項。)
                CVS = null,
                // 超商條碼繳費啟用(1=啟用、0或者未有此參數，即代表不開啟)(當該筆訂單金額小於 20 元或超過 4 萬元時，即使此參數設定為啟用，MPG 付款頁面仍不會顯示此支付方式選項。)
                BARCODE = null
            };

            if (string.Equals(payType, "CREDIT"))
            {
                newebPayRequest.CREDIT = 1;
            }
            else if (string.Equals(payType, "WEBATM"))
            {
                newebPayRequest.WEBATM = 1;
            }
            else if (string.Equals(payType, "VACC"))
            {
                // 設定繳費截止日期
                newebPayRequest.ExpireDate = taipeiStandardTimeOffset.AddDays(1).ToString("yyyyMMdd");
                newebPayRequest.VACC = 1;
            }
            else if (string.Equals(payType, "CVS"))
            {
                // 設定繳費截止日期
                newebPayRequest.ExpireDate = taipeiStandardTimeOffset.AddHours(1).ToString("yyyyMMdd");
                newebPayRequest.CVS = 1;
            }
            else if (string.Equals(payType, "BARCODE"))
            {
                // 設定繳費截止日期
                newebPayRequest.ExpireDate = taipeiStandardTimeOffset.AddHours(1).ToString("yyyyMMdd");
                newebPayRequest.BARCODE = 1;
            }

            var inputModel = new NewebPayBase
            {
                MerchantID = _appSettings.NewebPayMerchantID,
                Version = version
            };

            // 將model 轉換為List<KeyValuePair<string, string>>, null值不轉
            List<KeyValuePair<string, string>> tradeData = LambdaUtil.ModelToKeyValuePairList<NewebPayRequest>(newebPayRequest);
            // 將List<KeyValuePair<string, string>> 轉換為 key1=Value1&key2=Value2&key3=Value3...
            var tradeQueryPara = string.Join("&", tradeData.Select(x => $"{x.Key}={x.Value}"));
            // AES 加密
            inputModel.TradeInfo = CryptoUtil.EncryptAESHex(tradeQueryPara, _appSettings.NewebPayHashKey, _appSettings.NewebPayHashIV);
            // SHA256 加密
            inputModel.TradeSha = CryptoUtil.EncryptSHA256($"HashKey={_appSettings.NewebPayHashKey}&{inputModel.TradeInfo}&HashIV={_appSettings.NewebPayHashIV}");

            return inputModel;
        }
    }
}
