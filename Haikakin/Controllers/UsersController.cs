using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        [AllowAnonymous]
        [HttpPost("Authenticate")]
        public IActionResult Authenticate([FromBody] AuthenticationModel model)
        {
            var user = _userRepo.Authenticate(model.Email, model.Password, LoginTypeEnum.Normal);
            if (user == null)
            {
                return BadRequest(new { message = "UserEmail or Password is incorrect" });
            }

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            //檢查EMAIL有沒有使用過
            if (!_userRepo.IsUniqueUser(model.Email))
            {
                return BadRequest(new { message = "User Email already exists." });
            }

            var smsModel = _smsRepo.GetSmsModel(model.PhoneNumber);
            //檢查手機有沒有被使用過，理論上不會
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

            var user = _userRepo.Register(model);

            if (user == null)
            {
                return BadRequest(new { message = "Error while registering." });
            }

            smsModel.IsUsed = true;
            if (_smsRepo.UpdateSmsModel(smsModel))
            {
                return BadRequest(new { message = "Unknown error." });
            };

            //補寄信流程
            //https://localhost/api/?data=123456789&data2=123456789 範例網址
            var id = Encrypt.AesDecryptBase64(user.Id.ToString(), _appSettings.EmailSecret);
            var email = Encrypt.AesDecryptBase64(user.Email, _appSettings.EmailSecret);
            string url = $"";
            string mailBody = $"親愛的使用者 {model.Username}，你的信箱驗證網址為: ${url}";

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("SignByGoogle")]
        public async Task<IActionResult> RegisterAndLoginByThird([FromBody] AuthenticationThirdModel model)
        {

            if (model.LoginType != LoginTypeEnum.Google)
            {
                return BadRequest(new { message = "Not supported login." });
            }

            var token = model.TokenId;
            var userName = "";
            var userEmail = "";
            //產生抓取應用程式Token
            var url = "https://oauth2.googleapis.com";
            var urlReq = $"tokeninfo?id_token={token}";
            Console.WriteLine($"url={url}");
            HttpClient client = new HttpClient() { BaseAddress = new Uri(url) };
            //使用 async 方法從網路 url 上取得回應
            using (HttpResponseMessage response = await client.GetAsync(urlReq))
            using (HttpContent content = response.Content)
            {
                string result = await content.ReadAsStringAsync();
                if (result != null)
                {
                    var userObj = JObject.Parse(result);
                    var msg = Convert.ToString(userObj["error"]);
                    if (msg == "invalid_token")
                    {
                        return BadRequest(new { message = "Token has problem." });
                    }
                    else
                    {
                        userEmail = Convert.ToString(userObj["email"]);
                        userName = Convert.ToString(userObj["name"]);
                    }
                }
                else
                {
                    return BadRequest(new { message = "No response." });
                }
            }

            //檢查有沒有使用信箱 沒信箱或名字不給辦
            if (userName == string.Empty || userEmail == string.Empty)
            {
                return BadRequest(new { message = "Can't get name or email." });
            }
            //檢查有無重複帳號
            if (_userRepo.IsUniqueUser(userEmail))
            {
                //有重複帳號回傳JWT TOEKN
                var user = _userRepo.AuthenticateThird(userEmail, LoginTypeEnum.Google);
                return Ok(user);
            }
            else
            {
                //創帳號 回傳TOKEN
                _userRepo.RegisterThird(userName, userEmail, LoginTypeEnum.Google);
                var user = _userRepo.AuthenticateThird(userEmail, LoginTypeEnum.Google);
                return Ok(user);
            }
        }

        [AllowAnonymous]
        [HttpGet("EmailVerity")]
        public IActionResult CheckEmail(string uid, string email)
        {
            var decryptUid = int.Parse(Encrypt.AesDecryptBase64(uid, _appSettings.EmailSecret));
            var decryptEmail = Encrypt.AesDecryptBase64(email, _appSettings.EmailSecret);

            var user = _userRepo.GetUser(decryptUid);

            if (user == null)
            {
                return BadRequest(new { message = "Not found user." });
            }

            if (user.Id != decryptUid || user.Email != decryptEmail)
            {
                return BadRequest(new { message = "Not correct user." });
            }

            user.EmailVerity = true;
            _userRepo.UpdateUser(user);

            return Ok();
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
