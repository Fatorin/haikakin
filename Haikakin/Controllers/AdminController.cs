using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Haikakin.Models;
using Haikakin.Models.OrderModel;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

        public AdminController(IUserRepository userRepo, IOrderRepository orderRepo, IOrderInfoRepository orderInfoRepo, IProductRepository productRepo, IProductInfoRepository productInfoRepo, IOptions<AppSettings> appSettings)
        {
            _userRepo = userRepo;
            _orderRepo = orderRepo;
            _orderInfoRepo = orderInfoRepo;
            _productRepo = productRepo;
            _productInfoRepo = productInfoRepo;
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

        [HttpGet("GetUsersByAdmin")]
        [ProducesResponseType(200, Type = typeof(User))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUsersByAdmin()
        {
            var list = _userRepo.GetUsers();

            return Ok(list);
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
        public async Task<IActionResult> GetReport([FromBody] DateTime queryStartTime, DateTime queryLastTime)
        {
            if (queryStartTime == null || queryLastTime == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "日期時間錯誤" });
            }

            var objList = _orderRepo.GetOrdersWithTimeRange(queryStartTime, queryLastTime);

            var workbook = await GenerateReport(objList);

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
                            var firstModel = new ReportModel
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
                            var otherModel = new ReportModel
                            {
                                OrderItems = $"{tempProduct.ProductName}x{info.Count}",
                                OrderCounts = $"{info.Count}",
                                OrderAmounts = $"{Convert.ToDecimal(info.Count) * tempProduct.Price}",
                                Exchange = order.Exchange,
                            };
                            voteResult.Add(otherModel);
                        }
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
