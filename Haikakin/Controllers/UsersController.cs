﻿using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Google.Apis.Auth;
using Haikakin.Extension;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Models.MailModel;
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
    [Route("api/v{version:apiVersion}/Users")]
    [ApiController]
    public class UsersController : ControllerBase
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
        /// 更新用戶名稱，Admin要打完整ID才有用
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns></returns>
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpPost("UpdateUserName")]
        public IActionResult UpdateUserName(UserNameUpdateDto userDto)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "身份驗證異常" });
            }

            if (userDto == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料異常" });
            }

            //如果有不是管理者則自己身ID為主，此時傳值無效
            var role = identity.FindFirst(ClaimTypes.Role).Value;
            if (role != "Admin")
            {
                userDto.UserId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
            }

            var user = _userRepo.GetUser(userDto.UserId);

            if (user == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的使用者" });
            }

            user.Username = userDto.Username;
            if (!_userRepo.UpdateUser(user))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = $"資料更新錯誤:{userDto.UserId}" });
            }

            return NoContent();
        }

        /// <summary>
        /// 更新用戶密碼，Admin要打完整ID才有用
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns></returns>
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpPost("UpdateUserPassword")]
        public IActionResult UpdateUserPassword(UserPasswordUpdateDto userDto)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "身份驗證異常" });
            }

            if (userDto == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料異常" });
            }

            //如果有不是管理者則自己身ID為主，此時傳值無效
            var role = identity.FindFirst(ClaimTypes.Role).Value;
            if (role != "Admin")
            {
                userDto.UserId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
            }

            var user = _userRepo.GetUser(userDto.UserId);

            if (user == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的使用者" });
            }

            if (user.LoginType != LoginTypeEnum.Normal)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "第三方登入不支援改密碼" });
            }

            var oldPasswordConvert = Encrypt.HMACSHA256(userDto.UserOldPassword, _appSettings.UserSecret);
            if (oldPasswordConvert != user.Password)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "舊密碼不對" });
            }

            if (userDto.UserPassword != userDto.UserReCheckPassword)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "新密碼不一致" });
            }

            var newPassword = Encrypt.HMACSHA256(userDto.UserPassword, _appSettings.UserSecret);

            user.Password = newPassword;
            if (_userRepo.UpdateUser(user))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = $"資料更新錯誤:{userDto.UserId}" });
            }

            return NoContent();
        }

        /// <summary>
        /// 更新用戶信箱，Admin要打完整ID才有用
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns></returns>
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpPost("UpdateUserEmail")]
        public IActionResult UpdateUserEmail(UserEmailUpdateDto userDto)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "身份驗證異常" });
            }

            if (userDto == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料異常" });
            }

            //如果有不是管理者則自己身ID為主，此時傳值無效
            var role = identity.FindFirst(ClaimTypes.Role).Value;
            if (role != "Admin")
            {
                userDto.UserId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
            }

            var user = _userRepo.GetUser(userDto.UserId);

            if (user == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的使用者" });
            }

            if (user.Email == userDto.UserEmail)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "相同的信箱" });
            }

            user.EmailVerity = false;
            user.Email = userDto.UserEmail;

            if (!_userRepo.UpdateUser(user))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = $"資料更新錯誤:{userDto.UserId}" });
            }

            //寄驗證信
            var service = new SendMailService(_appSettings.MailgunAPIKey);
            EmailAccount mailModel = new EmailAccount($"{user.UserId}", user.Username, user.Email, EmailVerityModel.EmailVerityEnum.EmailModify);
            if (!service.AccountMailBuild(mailModel))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "信件系統異常，可能無法收信" });
            };

            return NoContent();
        }


        /// <summary>
        ///驗證身份並獲得Token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthenticateResponse))]
        [HttpPost("Authenticate")]
        public IActionResult Authenticate([FromBody] AuthenticationModel model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料異常" });
            }

            var response = _userRepo.Authenticate(model, LoginTypeEnum.Normal, GetIPAddress());

            if (response == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "帳號/手機或密碼不正確" });
            }

            SetTokenCookie(response.RefreshToken);

            return Ok(response);
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

        /// <summary>
        /// 註冊會員
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("Register")]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料異常" });
            }

            //檢查EMAIL有沒有使用過
            if (!_userRepo.IsUniqueUser(model.Email))
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "信箱已使用過" });
            }

            //檢查驗證模組裡面有沒有對應的號碼
            var smsModel = _smsRepo.GetSmsModel(model.PhoneNumber);
            if (smsModel == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "手機號碼尚未驗證" });
            }

            if (smsModel.IsUsed)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "手機號碼已註冊過" });
            }

            //檢查驗證碼對不對
            if (smsModel.VerityCode != model.SmsCode)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "驗證碼不正確" });
            }

            //註冊寫入db
            var user = _userRepo.Register(model);

            if (user == null)
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統註冊異常" });
            }

            //修改sms的資料
            smsModel.IsUsed = true;
            if (!_smsRepo.UpdateSmsModel(smsModel))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統更新驗證碼資訊異常" });
            };

            //補寄信流程
            SendMailService service = new SendMailService(_appSettings.MailgunAPIKey);
            EmailAccount mailModel = new EmailAccount($"{user.UserId}", user.Username, user.Email, EmailVerityModel.EmailVerityEnum.EmailVerity);
            if (!service.AccountMailBuild(mailModel))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "信件系統異常，可能無法收信" });
            };

            return Ok();
        }

        /// <summary>
        /// 註冊會員(Google)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthenticateResponse))]
        [HttpPost("SignByGoogle")]
        public async Task<IActionResult> RegisterAndLoginByThird([FromBody] AuthenticationThirdModel model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "資料請求錯誤" });
            }

            if (model.LoginType != LoginTypeEnum.Google)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不支援的登入方式" });
            }

            //產生抓取應用程式Token
            var payload = await GoogleJsonWebSignature.ValidateAsync(model.TokenId).ConfigureAwait(true);
            if (payload == null)
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "Google驗證異常" });
            }

            var userEmail = payload.Email;
            var userName = payload.Name;
            //檢查有沒有使用信箱 沒信箱或名字不給辦
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userEmail))
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "Google帳戶沒有信箱或用戶名" });
            }

            //檢查有無重複帳號
            if (_userRepo.IsUniqueUser(userEmail))
            {
                //創帳號 回傳TOKEN
                _userRepo.RegisterThird(userName, userEmail, LoginTypeEnum.Google);
                var response = _userRepo.AuthenticateThird(userEmail, LoginTypeEnum.Google, GetIPAddress());
                SetTokenCookie(response.RefreshToken);
                return Ok(response);
            }
            else
            {
                //有重複帳號回傳JWT TOEKN
                var response = _userRepo.AuthenticateThird(userEmail, LoginTypeEnum.Google, GetIPAddress());
                SetTokenCookie(response.RefreshToken);
                return Ok(response);
            }
        }

        /// <summary>
        /// 信箱驗證含信箱修改
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("EmailVerity")]
        public IActionResult EmailVerity([FromBody] EmailVerityModel model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "資料傳送錯誤" });
            }

            var user = _userRepo.GetUser(model.UserId);
            if (user == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "沒有此用戶" });
            }

            if (user.EmailVerity == true)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "信箱已驗證過" });
            }

            if (user.UserId != model.UserId || user.Email != model.UserEmail)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "用戶資訊不正確" });
            }

            //如果是要驗證
            if (model.EmailVerityAction == EmailVerityModel.EmailVerityEnum.EmailVerity)
            {
                user.EmailVerity = true;
                if (!_userRepo.UpdateUser(user))
                {
                    return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統驗證信箱異常" });
                }

                return Ok();
            }

            //如果是要修改
            if (model.EmailVerityAction == EmailVerityModel.EmailVerityEnum.EmailModify)
            {
                if (user.Email != model.UserEmail)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "信箱資訊不正確" });
                }
                user.EmailVerity = true;
                if (!_userRepo.UpdateUser(user))
                {
                    return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統修改信箱異常" });
                }

                return Ok();
            }

            return BadRequest(new { message = "未知的錯誤" });
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

        //FB登入支援，但沒在用
        private async Task<bool> GetFacebookUserData(string token)
        {
            //產生抓取應用程式Token
            var appId = _appSettings.FacebookAppId;
            var appSecret = _appSettings.FacebookAppSecret;
            var graphUrl = "https://graph.facebook.com";

            var userToken = token;
            var appToken = "";

            var userEmail = "";
            var userName = "";

            var urlReq = $"oauth/access_token?client_id={appId}&client_secret={appSecret}&grant_type=client_credentials";
            HttpClient client = new HttpClient() { BaseAddress = new Uri(graphUrl) };
            using (HttpResponseMessage response = await client.GetAsync(urlReq))
            using (HttpContent content = response.Content)
            {
                string result = await content.ReadAsStringAsync();
                if (result != null)
                {
                    var userObj = JObject.Parse(result);
                    appToken = Convert.ToString(userObj["access_token"]);
                }
                else
                {
                    return false;
                }
            }

            //檢查TOKEN是否合法
            var compareUrl = $"debug_token?input_token={userToken}&access_token={appToken}";
            using (HttpResponseMessage response = await client.GetAsync(compareUrl))
            using (HttpContent content = response.Content)
            {
                string result = await content.ReadAsStringAsync();
                if (result != null)
                {
                    var userObj = JObject.Parse(result);
                    var isValid = Convert.ToBoolean(userObj["data"]["is_valid"]);

                    if (!isValid) return false;
                }
                else
                {
                    return false;
                }
            }

            //可抓資料 可獲得id、name、email分別帶入userobj裡面
            //有問題就回傳錯誤
            var getUserDataUrl = $"me?fields=id,name,email&access_token={userToken}";
            using (HttpResponseMessage response = await client.GetAsync(getUserDataUrl))
            using (HttpContent content = response.Content)
            {
                string result = await content.ReadAsStringAsync();
                if (result != null)
                {
                    var userObj = JObject.Parse(result);
                    userName = Convert.ToString(userObj["name"]);
                    userEmail = Convert.ToString(userObj["email"]);
                }
                else
                {
                    return false;
                }
            }
            //檢查有沒有使用信箱 沒信箱不給辦
            if (userName == string.Empty || userEmail == string.Empty)
            {
                return false;
            }
            //檢查有無重複帳號
            if (_userRepo.IsUniqueUser(userEmail)) return false;
            //創帳號
            _userRepo.RegisterThird(userName, userEmail, LoginTypeEnum.Facebook);

            return true;
        }

    }
}
