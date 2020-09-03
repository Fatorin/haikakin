using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using Haikakin.Extension;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Models.OrderScheduler;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
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
        private IUserRepository _userRepo;
        private readonly AppSettings _appSettings;

        public SmsController(ISmsRepository smsRepo, IUserRepository userRepo, IOptions<AppSettings> appSettings)
        {
            _smsRepo = smsRepo;
            _userRepo = userRepo;
            _appSettings = appSettings.Value;
        }

        /// <summary>
        /// 簡訊驗證手機
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("SmsAuthenticate")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult SmsVerityCode([FromBody] SmsModelDto model)
        {
            if (string.IsNullOrEmpty(model.PhoneNumber))
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "沒有輸入電話號碼" });
            }
            //檢查該手機號碼是否註冊過
            var smsModel = _smsRepo.GetSmsModel(model.PhoneNumber);
            if (smsModel != null)
            {
                if (smsModel.IsUsed) return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "號碼已註冊過" });
            }
            //產生驗證用字串
            var randomString = SmsRandomNumber.CreatedNumber();

            //如果有回應則分析回傳結果
            if (!SendMitakeSms(model.PhoneNumber, randomString, model.isTaiwanNumber))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統簡訊發送異常" });
            }

            //抓一下有沒有已存在的
            if (smsModel == null)
            {
                //沒有就幫他建立一個新的
                smsModel = new SmsModel
                {
                    PhoneNumber = model.PhoneNumber,
                    VerityCode = randomString,
                    VerityLimitTime = DateTime.UtcNow.AddMinutes(5)
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
                smsModel.VerityLimitTime = DateTime.UtcNow.AddMinutes(5);
                if (!_smsRepo.UpdateSmsModel(smsModel))
                {
                    return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統更新驗證碼出錯" });
                }
            }

            return Ok();
        }


        private bool SendTwiloSms(string randomString, string phoneNumber)
        {
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
                    return false;
                }
            }
            catch (TwilioException e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        private bool SendMitakeSms(string phoneNumber, string randomString, bool isTaiwanNumber)
        {
            StringBuilder reqUrl = new StringBuilder();
            reqUrl.Append("https://smsapi.mitake.com.tw/api/mtk/SmSend?&CharsetURL=UTF-8");

            StringBuilder reqData = new StringBuilder();
            reqData.Append($"username={_appSettings.MitakeSmsAccountID}");
            reqData.Append($"&password={_appSettings.MitakeSmsAccountPassword}");

            if (isTaiwanNumber)
            {
                string modifyToTaiwanFormatNumber = $"0{phoneNumber.Substring(3)}";
                reqData.Append($"&dstaddr={modifyToTaiwanFormatNumber}");
            }
            else
            {
                reqData.Append($"&dstaddr={phoneNumber}");
            }

            reqData.Append($"&smbody=Your Haikakin Web Services verification code is:{randomString}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(reqUrl.ToString()));
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            byte[] bs = Encoding.UTF8.GetBytes(reqData.ToString());
            request.ContentLength = bs.Length;
            request.GetRequestStream().Write(bs, 0, bs.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());
            string result = sr.ReadToEnd();

            string resultCodeString = "statuscode";
            int start = result.IndexOf(resultCodeString) + resultCodeString.Length + 1;

            int resultSuccess = int.Parse(result.Substring(1, 1));
            int resultCode = int.Parse(result.Substring(start, 1));

            if (resultSuccess != 1)
            {
                return false;
            }

            if (resultCode > 4 || resultCode < 0)
            {
                return false;
            }

            if (resultCode > 4 || resultCode < 0)
            {
                return false;
            }

            return true;
        }

    }
}
