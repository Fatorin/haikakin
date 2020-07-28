using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Haikakin.Extension;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Twilio.TwiML.Voice;
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
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UsersController(IUserRepository userRepo, ISmsRepository smsRepo, IOptions<AppSettings> appSettings, IMapper mapper)
        {
            _userRepo = userRepo;
            _smsRepo = smsRepo;
            _appSettings = appSettings.Value;
            _mapper = mapper;
        }

        /// <summary>
        /// 要求指定用戶資料
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("GetUser")]
        public IActionResult GetUser(int userId)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new { message = "Bad token" });
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
                return NotFound();
            }

            user.Password = "";

            return Ok(user);
        }

        /// <summary>
        /// 更新用戶資料
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns></returns>
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("UpdateUser")]
        public IActionResult UpdateUser(UserUpdateDto userDto)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
            {
                return BadRequest(new { message = "Bad token" });
            }

            if (userDto == null)
            {
                return BadRequest(ModelState);
            }

            //如果有不是管理者則自己身ID為主，此時傳值無效
            var role = identity.FindFirst(ClaimTypes.Role).Value;
            if (role != "Admin")
            {
                userDto.UserId = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
            }

            var userObj = _mapper.Map<User>(userDto);

            if (_userRepo.UpdateUser(userObj))
            {
                return StatusCode(500, ModelState);
            }

            return Ok();
        }

        /// <summary>
        ///驗證身份並獲得Token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("Authenticate")]
        public IActionResult Authenticate([FromBody] AuthenticationModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "傳值錯誤" });
            }

            var response = _userRepo.Authenticate(model, LoginTypeEnum.Normal, GetIPAddress());

            if (response == null)
            {
                return BadRequest(new { message = "UserEmail or Password is incorrect" });
            }

            SetTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        /// <summary>
        /// 刷新Token用
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public IActionResult RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _userRepo.RefreshToken(refreshToken, GetIPAddress());

            if (response == null)
                return Unauthorized(new { message = "Invalid token" });

            SetTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        /// <summary>
        /// 撤銷Token用
        /// </summary>
        /// <returns></returns>
        [HttpPost("revoke-token")]
        public IActionResult RevokeToken([FromBody] string requestToken)
        {
            // accept token from request body or cookie
            var token = requestToken ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            var response = _userRepo.RevokeToken(token, GetIPAddress());

            if (!response)
                return NotFound(new { message = "Token not found" });

            return Ok(new { message = "Token revoked" });
        }

        /// <summary>
        /// 註冊會員
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("Register")]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            //檢查EMAIL有沒有使用過
            if (!_userRepo.IsUniqueUser(model.Email))
            {
                return BadRequest(new { message = "User Email already exists." });
            }

            //檢查驗證模組裡面有沒有對應的號碼
            var smsModel = _smsRepo.GetSmsModel(model.PhoneNumber);
            if (smsModel == null)
            {
                return BadRequest(new { message = "Phone number not verification." });
            }

            if (smsModel.IsUsed)
            {
                return BadRequest(new { message = "Phone number was used." });
            }

            //檢查驗證碼對不對
            if (smsModel.VerityCode != model.SmsCode)
            {
                return BadRequest(new { message = "SmsCode is wrong." });
            }

            //註冊寫入db
            var user = _userRepo.Register(model);

            if (user == null)
            {
                return BadRequest(new { message = "Error while registering." });
            }

            //修改sms的資料
            smsModel.IsUsed = true;
            if (!_smsRepo.UpdateSmsModel(smsModel))
            {
                return BadRequest(new { message = "Unknown error." });
            };

            //補寄信流程
            SendMail(user.UserId.ToString(), user.Email);

            return Ok();
        }

        /// <summary>
        /// 註冊會員(Google)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("SignByGoogle")]
        public async Task<IActionResult> RegisterAndLoginByThird([FromBody] AuthenticationThirdModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "沒有接收到資料" });
            }

            if (model.LoginType != LoginTypeEnum.Google)
            {
                return BadRequest(new { message = "Not supported login." });
            }

            var userName = "";
            var userEmail = "";
            //產生抓取應用程式Token

            var payload = await GoogleJsonWebSignature.ValidateAsync(model.TokenId).ConfigureAwait(true);
            if (payload == null)
            {
                return BadRequest(new { message = "Google valid fail." });
            }

            userEmail = payload.Email;
            userName = payload.Name;

            //檢查有沒有使用信箱 沒信箱或名字不給辦
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userEmail))
            {
                return BadRequest(new { message = "Can't get name or email." });
            }

            //檢查有無重複帳號
            if (_userRepo.IsUniqueUser(userEmail))
            {
                //創帳號 回傳TOKEN
                _userRepo.RegisterThird(userName, userEmail, LoginTypeEnum.Google);
                var user = _userRepo.AuthenticateThird(userEmail, LoginTypeEnum.Google,GetIPAddress());
                return Ok(user);
            }
            else
            {
                //有重複帳號回傳JWT TOEKN
                var user = _userRepo.AuthenticateThird(userEmail, LoginTypeEnum.Google, GetIPAddress());
                return Ok(user);
            }
        }

        /// <summary>
        /// 信箱驗證含信箱修改
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet("EmailVerity")]
        public IActionResult EmailVerity([FromBody] EmailVerityModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "沒有接收到資料" });
            }

            var user = _userRepo.GetUser(model.userId);
            if (user.EmailVerity == true)
            {
                return BadRequest(new { message = "Has verified" });
            }

            if (user == null)
            {
                return BadRequest(new { message = "Not found user." });
            }

            if (user.UserId != model.userId || user.Email != model.userEmail)
            {
                return BadRequest(new { message = "Not correct user." });
            }

            //如果是要驗證
            if (model.emailVerityAction == EmailVerityModel.EmailVerityAction.EmailVerity)
            {
                user.EmailVerity = true;
                if (!_userRepo.UpdateUser(user))
                {
                    return BadRequest(new { message = "Unknown Error." });
                }

                return Ok();
            }

            //如果是要驗證
            if (model.emailVerityAction == EmailVerityModel.EmailVerityAction.EmailModify)
            {
                user.Email = model.userEmail;
                user.EmailVerity = true;
                if (!_userRepo.UpdateUser(user))
                {
                    return BadRequest(new { message = "Unknown Error." });
                }

                return Ok();
            }

            return BadRequest(new { message = "Unknown Error." });
        }

        private bool SendMail(string uId, string userEmail)
        {
            var titleText = "Haikakin 會員驗證信";
            var mailUrl = $"https://haikakin.com/emailcheck/?uid={uId}&email={userEmail}";
            var titleBody = $"親愛的使用者，你的信箱驗證網址為: {mailUrl}";

            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");
            client.Authenticator =
                new HttpBasicAuthenticator("api",
                                            _appSettings.MailgunAPIKey);
            RestRequest request = new RestRequest();
            request.AddParameter("domain", "mail.haikakin.com", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", "Haikakin Service <service@mail.haikakin.com>");
            request.AddParameter("to", $"{userEmail}");
            request.AddParameter("subject", titleText);
            request.AddParameter("text", titleBody);
            request.Method = Method.POST;
            var response = client.Execute(request);
            return response.IsSuccessful;
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
