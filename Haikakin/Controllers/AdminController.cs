using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Google.Apis.Auth;
using Haikakin.Extension;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Models.MailModel;
using Haikakin.Models.OrderModel;
using Haikakin.Models.UserModel;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
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

        /// <summary>
        /// 查詢指定訂單，Admin限定
        /// </summary>
        /// <param name="orderId"> The id of the order</param>
        /// <returns></returns>
        [HttpGet("{orderId:int}", Name = "GetOrderByAdmin")]
        [ProducesResponseType(200, Type = typeof(OrderResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
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
                var serialList = _productInfoRepo.GetProductInfosByOrderInfoId(orderInfo.OrderInfoId).Select(x=>x.Serial).ToList();
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
                OrderPrice = decimal.ToInt32(obj.OrderPrice),
                OrderInfos = orderInfoRespList,
                UserId = user.UserId,
                UserEmail = user.Email,
                UserName = user.Username
            };

            return Ok(orderRespModel);
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

    }
}
