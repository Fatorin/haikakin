using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Google.Apis.Auth;
using Haikakin.Extension;
using Haikakin.Extension.Services;
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
    public partial class UsersController : ControllerBase
    {
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
            if (!_userRepo.UpdateUser(user))
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
        [ProducesResponseType(StatusCodes.Status200OK)]
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

            var emailUser = _userRepo.GetUserByEmail(userDto.UserEmail);
            if (emailUser != null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "已經有人使用過" });
            }

            //寄驗證信
            var service = new SendMailService(_appSettings.MailgunAPIKey);
            EmailAccount mailModel = new EmailAccount($"{user.UserId}", user.Username, user.Email, EmailVerityModel.EmailVerityEnum.EmailModify);
            if (!service.AccountMailBuild(mailModel))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "信件系統異常，可能無法收信" });
            };

            return Ok();
        }

        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("ForgetPassword")]
        public IActionResult ForgetPassword(ForgetPasswordDto model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料異常" });
            }

            var user = _userRepo.GetUserByEmail(model.UserEmail);

            if (user == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的使用者" });
            }

            var randomPasswordString = StringExtension.GenRandomPassword(10);

            var passwordConvertToDatabase = Encrypt.HMACSHA256(randomPasswordString, _appSettings.UserSecret);

            user.Password = passwordConvertToDatabase;

            if (!_userRepo.UpdateUser(user))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = $"資料更新錯誤:{user.UserId}" });
            }

            //寄驗證信
            var service = new SendMailService(_appSettings.MailgunAPIKey);
            var mailModel = new EmailForgetPasswordModel()
            {
                UserEmail = user.Email,
                UserName = user.Username,
                UserPassword = randomPasswordString,
            };
            if (!service.ForgetPasswordMailBuild(mailModel))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "信件系統異常，可能無法收信" });
            };

            return Ok();
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

            //如果是要驗證
            if (model.EmailVerityAction == EmailVerityModel.EmailVerityEnum.EmailVerity)
            {
                if (user.Email != model.UserEmail)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "驗證信箱不正確" });
                }

                if (user.EmailVerity == true)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "信箱已驗證過" });
                }

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
                var emailUser = _userRepo.GetUserByEmail(model.UserEmail);
                if (emailUser != null)
                {
                    return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "已經有人使用過" });
                }

                user.EmailVerity = true;
                user.Email = model.UserEmail;
                if (!_userRepo.UpdateUser(user))
                {
                    return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "系統修改信箱異常" });
                }

                return Ok();
            }

            return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資訊異常" });
        }
    }
}
