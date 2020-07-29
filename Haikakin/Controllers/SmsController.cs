using System;
using Haikakin.Extension;
using Haikakin.Models;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/Sms")]
    [ApiController]
    public class SmsController : ControllerBase
    {
        private ISmsRepository _smsRepo;
        private readonly AppSettings _appSettings;

        public SmsController(ISmsRepository smsRepo, IOptions<AppSettings> appSettings)
        {
            _smsRepo = smsRepo;
            _appSettings = appSettings.Value;
        }

        /// <summary>
        /// 簡訊驗證手機
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("SmsAuthenticate")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult SmsVerityCode(string phoneNumber)
        {
            //檢查該手機號碼是否註冊過
            var smsModel = _smsRepo.GetSmsModel(phoneNumber);
            if (smsModel != null)
            {
                if (smsModel.IsUsed) return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "號碼已註冊過" });
            }
            //產生驗證用字串
            var randomString = SmsRandomNumber.CreatedNumber();

            //如果有回應則分析回傳結果
            TwilioClient.Init(_appSettings.TwilioSmsAccountID, _appSettings.TwilioSmsAuthToken);

            try
            {
                var msg = MessageResource.Create(
                        body: $"Your Haikakin Web Services verification code is:{randomString}",
                        from: new Twilio.Types.PhoneNumber($"+12058138320"),
                        to: new Twilio.Types.PhoneNumber($"+{phoneNumber}")
                );

                //確認回傳有無錯誤
                if (msg.ErrorCode != null)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "號碼不正確或簡訊發送商異常" });
                }
            }
            catch (TwilioException e)
            {
                Console.WriteLine(e);
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "號碼不正確或簡訊發送商異常" });
            }

            //抓一下有沒有已存在的
            if (smsModel == null)
            {
                //沒有就幫他建立一個新的
                smsModel = new SmsModel
                {
                    PhoneNumber = phoneNumber,
                    VerityCode = randomString,
                    VerityLimitTime = DateTime.UtcNow.AddDays(1)
                };

                if (!_smsRepo.CreateSmsModel(smsModel))
                {
                    return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統建立驗證碼出錯" });
                }
            }
            else
            {
                //更新驗證碼
                smsModel.VerityCode = randomString;
                if (!_smsRepo.UpdateSmsModel(smsModel))
                {
                    return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統更新驗證碼出錯" });
                }
            }

            return Ok();
        }

        /*
         * 三竹簡訊版本，未開通無法使用
         * 
        [AllowAnonymous]
        [HttpPost("SmsAuthenticate")]
        public IActionResult SmsVerityCodeExtra(string phoneNumber)
        {
            //檢查該手機號碼是否註冊過
            var smsModel = _smsRepo.GetSmsModel(phoneNumber);
            if (smsModel != null)
            {
                if (smsModel.IsUsed) return BadRequest(new { message = "Number was used." });
            }
            //產生驗證用字串
            var randomString = SmsRandomNumber.CreatedNumber();

            //產出簡訊服務
            StringBuilder reqUrl = new StringBuilder();
            reqUrl.Append("https://sms.mitake.com.tw/b2c/mtk/SmSend?&CharsetURL=UTF-8");
            StringBuilder smsParams = new StringBuilder();
            smsParams.Append($"username={_appSettings.SmsAccountID}");
            smsParams.Append($"&password={_appSettings.SmsAccountPassword}");
            smsParams.Append($"&dstaddr={phoneNumber}");
            smsParams.Append($"&smbody=Your Haikakin Web Services verification code is:{randomString}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new
            Uri(reqUrl.ToString()));
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            byte[] bs = Encoding.UTF8.GetBytes(smsParams.ToString());
            request.ContentLength = bs.Length;
            request.GetRequestStream().Write(bs, 0, bs.Length);
            //接收回應
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());
            string result = sr.ReadToEnd();
            //如果有回應則分析回傳結果
            if (result == null)
            {
                return BadRequest(new { message = "Sms send fail." });
            }
            //確認OK之後傳入DB裡面

            //抓一下有沒有已存在的
            if (smsModel == null)
            {
                //沒有就幫他建立一個新的
                smsModel = new SmsModel
                {
                    PhoneNumber = phoneNumber,
                    VerityCode = randomString,
                    VerityLimitTime = DateTime.UtcNow.AddDays(1)
                };

                if (!_smsRepo.CreateSmsModel(smsModel))
                {
                    return BadRequest(new { message = "Create smsCode fail." });
                }
            }
            else
            {
                //更新驗證碼
                smsModel.VerityCode = randomString;
                if (!_smsRepo.UpdateSmsModel(smsModel))
                {
                    return BadRequest(new { message = "Refresh smsCode fail." });
                }
            }

            return Ok();
        }*/

    }
}
