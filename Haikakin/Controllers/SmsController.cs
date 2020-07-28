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
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using static Haikakin.Models.User;

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
        public IActionResult SmsVerityCode(string phoneNumber)
        {
            //檢查該手機號碼是否註冊過
            var smsModel = _smsRepo.GetSmsModel(phoneNumber);
            if (smsModel != null)
            {
                if (smsModel.IsUsed) return BadRequest(new { message = "Number was used." });
            }
            //產生驗證用字串
            var randomString = SmsRandomNumber.CreatedNumber();

            //如果有回應則分析回傳結果
            TwilioClient.Init(_appSettings.TwilioSmsAccountID, _appSettings.TwilioSmsAuthToken);


            var msg = MessageResource.Create(
                    body: $"Your Haikakin Web Services verification code is:{randomString}",
                    from: new Twilio.Types.PhoneNumber($"+12058138320"),
                    to: new Twilio.Types.PhoneNumber($"+{phoneNumber}")
            );

            //確認回傳有無錯誤
            if (msg.ErrorCode != null)
            {
                return BadRequest(new { message = "Request smsCode fail, please check number or find support." });
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
