using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Haikakin.Extension;
using Haikakin.Extension.NewebPayUtil;
using Haikakin.Extension.Services;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Models.MailModel;
using Haikakin.Models.OrderModel;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using static Haikakin.Models.User;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/Admins")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private IUserRepository _userRepo;
        private IOrderRepository _orderRepo;
        private IOrderInfoRepository _orderInfoRepo;
        private IProductRepository _productRepo;
        private IProductInfoRepository _productInfoRepo;
        private readonly AppSettings _appSettings;
        private readonly IMemoryCache _memoryCache;

        public AdminController(IUserRepository userRepo, IOrderRepository orderRepo, IOrderInfoRepository orderInfoRepo, IProductRepository productRepo, IProductInfoRepository productInfoRepo, IOptions<AppSettings> appSettings, IMemoryCache memoryCache)
        {
            _userRepo = userRepo;
            _orderRepo = orderRepo;
            _orderInfoRepo = orderInfoRepo;
            _productRepo = productRepo;
            _productInfoRepo = productInfoRepo;
            _appSettings = appSettings.Value;
            _memoryCache = memoryCache;
        }

        /// <summary>
        ///驗證身份並獲得Token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthenticateResponse))]
        [HttpPost("AuthenticateAdmin")]
        public IActionResult AuthenticateAdmin([FromBody] AuthenticationModel model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料異常" });
            }

            var response = _userRepo.AuthenticateAdmin(model, LoginTypeEnum.Normal, GetIPAddress());

            if (response == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不是Admin、帳密錯誤、或是無此帳號" });
            }

            SetTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        /// <summary>
        /// ReCAPTCHA我不是機器人的驗證
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [HttpPost("AuthenticateReCAPTCHA")]
        [AllowAnonymous]
        public async Task<IActionResult> AuthenticateReCAPTCHA([FromBody] ReCAPTCHADto obj)
        {
            if (obj == null)
            {
                BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "沒收到Token" });
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string url = $"https://www.google.com/recaptcha/api/siteverify?secret=" +
                    $"{"6LcjBMgZAAAAAIEKaaH6UB17Vth4cv-Py8w82Ylb"}&" +
                    $"response={obj.Token}&" +
                    $"remoteip={HttpContext.Connection.RemoteIpAddress}";

                HttpResponseMessage response = await client.GetAsync(url);

                var data = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && BotRules(JsonConvert.DeserializeObject<ReCAPTCHA>(data)))
                {
                    return Ok(data);
                }
                else
                {
                    return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "toekn錯誤或是系統驗證異常" });
                }
            }
        }

        /// <summary>
        ///要求二階段驗證
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet("AuthenticateAdminTwoFaceRequest")]
        [Authorize(Roles = "Admin")]
        public IActionResult AuthenticateAdminTwoFaceRequest()
        {
            //要求驗證碼
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "身份驗證異常" });
            }

            var role = identity.FindFirst(ClaimTypes.Role).Value;

            if (role != "Admin")
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "身份驗證異常" });
            }

            int userId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);

            var user = _userRepo.GetUser(userId);
            var model = new EmailAdmin();
            model.UserName = user.Username;
            model.UserEmail = user.Email;
            //產生隨機驗證碼，存到快取裡面3分鐘
            var code = SmsRandomNumber.CreatedAdmin();
            _memoryCache.Set(userId, code, TimeSpan.FromMinutes(3));
            model.Code = code;
            var mailService = new SendMailService(_appSettings.MailgunAPIKey);
            if (!mailService.AdminMailBuild(model))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "信箱驗證異常" });
            }

            return Ok();
        }

        /// <summary>
        ///要求二階段驗證
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("AuthenticateAdminTwoFaceResponse")]
        [Authorize(Roles = "Admin")]
        public IActionResult AuthenticateAdminTwoFaceResponse([FromBody] string code)
        {
            //檢查傳進來的驗證碼
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "身份驗證異常" });
            }

            var role = identity.FindFirst(ClaimTypes.Role).Value;

            if (role != "Admin")
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "身份驗證異常" });
            }

            int userId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);

            if (!_memoryCache.TryGetValue(userId, out string value))
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "驗證碼已過期，請重新請求" });
            }

            if (value != code)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "驗證碼不一致，請重新輸入" });
            }

            return Ok();
        }

        [HttpGet("GetUsersByAdmin")]
        [ProducesResponseType(200, Type = typeof(User))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUsersByAdmin()
        {
            var list = _userRepo.GetUsers();

            return Ok(list);
        }

        [HttpPatch("UpdateUserBanStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateUserBanStatus([FromBody] UserBanUpdateDto model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料不正確" });
            }

            var user = _userRepo.GetUser(model.UserId);

            if (user == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "沒有該使用者" });
            }

            user.CheckBan = model.CheckBan;
            if (!_userRepo.UpdateUser(user))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統更新使用者異常" });
            }

            return Ok();
        }

        /// <summary>
        /// 查詢指定訂單，Admin限定
        /// </summary>
        /// <param name="orderId"> The id of the order</param>
        /// <returns></returns>
        [HttpGet("{orderId:int}", Name = "GetOrderByAdmin")]
        [ProducesResponseType(200, Type = typeof(OrderResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [Authorize(Roles = "Admin")]
        public IActionResult GetOrderByAdmin(int orderId)
        {
            var obj = _orderRepo.GetOrder(orderId);

            if (obj == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "訂單不存在" });
            }

            //檢查用戶是否存在
            var user = _userRepo.GetUser(obj.UserId);

            if (user == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "用戶不存在" });
            }

            var orderInfoRespList = new List<OrderInfoResponse>();
            foreach (var orderInfo in obj.OrderInfos)
            {
                var productName = _productRepo.GetProduct(orderInfo.ProductId).ProductName;
                var productInfos = new List<string>();
                var serialList = _productInfoRepo.GetProductInfosByOrderInfoId(orderInfo.OrderInfoId).Select(x => x.Serial).ToList();
                for (int i = 0; i < serialList.Count; i++)
                {
                    //解密
                    var key = CryptoUtil.DecryptAESHex(serialList[i], _appSettings.SerialHashKey, _appSettings.SerialHashIV);
                    //加密
                    serialList[i] = key.SerialEncrypt();
                }
                productInfos.AddRange(serialList);
                orderInfoRespList.Add(new OrderInfoResponse(productName, orderInfo.Count, serialList));
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
                UserIPAddress = user.IPAddress,
                UserEmail = user.Email,
                UserName = user.Username
            };

            return Ok(orderRespModel);
        }

        [HttpGet("GetReport")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        public async Task<IActionResult> GetReport(DateTime startTime, DateTime endTime, short? orderStatus)
        {
            var objList = _orderRepo.GetOrdersWithTimeRange(startTime, endTime, orderStatus);

            var workbook = await GenerateReport(objList).ConfigureAwait(true);

            if (workbook == null)
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "無法產生報表" });

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Close();
            return new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        //Token新機制
        private void SetTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string GetIPAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
        private bool BotRules(ReCAPTCHA data)
        {
            if (data.Success && data.Score >= 0.5) return true;
            else return false;
        }

        private async Task<XLWorkbook> GenerateReport(ICollection<Order> orders)
        {
            var t = Task.Run(() =>
            {
                var voteResult = new List<ReportModel>();
                foreach (var order in orders)
                {
                    var user = _userRepo.GetUser(order.UserId);
                    var orderInfos = _orderInfoRepo.GetOrderInfosByOrderId(order.OrderId);
                    bool flag = false;
                    foreach (var info in orderInfos)
                    {
                        //如果是第一個就建立全部，不是就建立分支
                        var tempProduct = _productRepo.GetProduct(info.ProductId);
                        if (!flag)
                        {
                            flag = true;
                            order.OrderFee = order.OrderFee ?? "0.0";
                            ReportModel firstModel = new ReportModel
                            {
                                OrderId = order.OrderId,
                                //商品第一項名稱
                                OrderItems = $"{tempProduct.ProductName}x{info.Count}",
                                OrderCounts = $"{info.Count}",
                                OrderAmounts = $"{Convert.ToDecimal(info.Count) * tempProduct.Price}",
                                UserEmail = user.Email,
                                UserName = user.Username,
                                OrderStatus = order.OrderStatus.ToString(),
                                BuyDate = order.OrderCreateTime.ToString("yyyy/MM/dd HH:mm:ss"),
                                PayDate = order.OrderLastUpdateTime.ToString("yyyy/MM/dd HH:mm:ss"),
                                OrderAllAmount = order.OrderAmount,
                                Exchange = order.Exchange,
                                PayWay = order.OrderPayWay.ToString(),
                                PayAmount = order.OrderAmount + decimal.Parse(order.OrderFee),
                                PayFee = decimal.Parse(order.OrderFee)
                            };
                            voteResult.Add(firstModel);
                        }
                        else
                        {
                            ReportModel otherModel = new ReportModel
                            {
                                OrderItems = $"{tempProduct.ProductName}x{info.Count}",
                                OrderCounts = $"{info.Count}",
                                OrderAmounts = $"{Convert.ToDecimal(info.Count) * tempProduct.Price}",
                                Exchange = order.Exchange,
                            };
                            voteResult.Add(otherModel);
                        }
                        flag = false;
                    }
                }

                var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("月報");
                ws.Cell(1, 1).Value = "訂單編號";
                ws.Cell(1, 2).Value = "品項";
                ws.Cell(1, 3).Value = "數量";
                ws.Cell(1, 4).Value = "進貨價格";
                ws.Cell(1, 5).Value = "電子郵件";
                ws.Cell(1, 6).Value = "付款人";
                ws.Cell(1, 7).Value = "購買日期";
                ws.Cell(1, 8).Value = "付款日期";
                ws.Cell(1, 9).Value = "商品總和";
                ws.Cell(1, 10).Value = "當日匯率";
                ws.Cell(1, 11).Value = "付款方式";
                ws.Cell(1, 12).Value = "交易金額";
                ws.Cell(1, 13).Value = "交易手續費";
                ws.Cell(2, 1).Value = voteResult;
                ws.Columns().AdjustToContents();

                return workbook;
            });

            return await t;
        }

    }
}
