using System;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Haikakin.Extension.Services;
using Haikakin.Models;
using Haikakin.Models.MailModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Haikakin.Models.User;

namespace Haikakin.Controllers
{
    public partial class UsersController : ControllerBase
    {
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

            if (smsModel.VerityLimitTime < DateTime.UtcNow)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "驗證碼已過期" });
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
    }
}
