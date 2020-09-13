using System;
using System.Security.Claims;
using Haikakin.Models;
using Haikakin.Models.UserModel;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/Users")]
    [ApiController]
    public partial class UsersController : ControllerBase
    {
        private IUserRepository _userRepo;
        private ISmsRepository _smsRepo;
        private readonly AppSettings _appSettings;

        public UsersController(IUserRepository userRepo, ISmsRepository smsRepo, IOptions<AppSettings> appSettings)
        {
            _userRepo = userRepo;
            _smsRepo = smsRepo;
            _appSettings = appSettings.Value;
        }

        /// <summary>
        /// 要求指定用戶資料
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(User))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [HttpGet("GetUser")]
        public IActionResult GetUser(int userId)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "身份驗證異常" });
            }

            //如果有不是管理者則自己身ID為主，此時傳值無效
            var role = identity.FindFirst(ClaimTypes.Role).Value;
            if (role != "Admin")
            {
                userId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
            }

            var user = _userRepo.GetUser(userId);

            if (user == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的使用者" });
            }

            user.Password = "";

            return Ok(user);
        }

        /// <summary>
        /// 刷新Token用
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthenticateResponse))]
        [HttpPost("refresh-token")]
        public IActionResult RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _userRepo.RefreshToken(refreshToken, GetIPAddress());

            if (response == null)
                return Unauthorized(new ErrorPack { ErrorCode = 1000, ErrorMessage = "Token錯誤" });

            SetTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        /// <summary>
        /// 撤銷Token用
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "User,Admin")]
        [HttpPost("revoke-token")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthenticateResponse))]
        public IActionResult RevokeToken([FromBody] RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "輸入資料錯誤" });

            var response = _userRepo.RevokeToken(token, GetIPAddress());

            if (!response)
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "沒有對應的Token使用者" });

            return Ok(new { message = "Token 已撤銷" });
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
            {
                return Request.Headers["X-Forwarded-For"];
            }
            else
            {
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            }
        }
    }
}
