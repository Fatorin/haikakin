using Haikakin.Data;
using Haikakin.Extension;
using Haikakin.Models;
using Haikakin.Repository.IRepository;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Haikakin.Models.AuthenticationThirdModel;
using static Haikakin.Models.User;

namespace Haikakin.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly AppSettings _appSettings;

        public UserRepository(ApplicationDbContext db, IOptions<AppSettings> appSettings)
        {
            _db = db;
            _appSettings = appSettings.Value;
        }


        public User Authenticate(string userEmail, string password, LoginTypeEnum loginType)
        {
            var encryptPassword = Encrypt.HMACSHA256(password);

            var user = _db.Users.SingleOrDefault(x => x.Email == userEmail && x.Password == encryptPassword && x.LoginType == loginType);

            if (user == null)
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.JwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name,user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);
            user.Password = "";
            user.LastLoginTime = DateTime.UtcNow;
            return user;
        }

        public User AuthenticateThird(string userEmail, LoginTypeEnum loginType)
        {
            var user = _db.Users.SingleOrDefault(x => x.Email == userEmail && x.LoginType == loginType);

            if (user == null)
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.JwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name,user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);
            user.Password = "";
            user.LastLoginTime = DateTime.UtcNow;
            return user;
        }

        public bool IsUniqueUser(string email)
        {
            var user = _db.Users.SingleOrDefault(x => x.Email == email);

            if (user == null) return true;

            return false;
        }

        public User Register(string username, string email, string password)
        {
            var encryptPassword = Encrypt.HMACSHA256(password);

            User userObj = new User()
            {
                Username = username,
                Email = email,
                Password = encryptPassword,
                Role = "User",
                CreateTime = DateTime.UtcNow,
                LoginType = LoginTypeEnum.Normal,
            };

            _db.Users.Add(userObj);
            _db.SaveChanges();
            userObj.Password = "";
            return userObj;
        }

        public User RegisterThird(string username, string email, LoginTypeEnum loginType)
        {
            User userObj = new User()
            {
                Username = username,
                Email = email,
                EmailVerity = true,
                Role = "User",
                CreateTime = DateTime.UtcNow,
                LoginType = loginType,
            };

            _db.Users.Add(userObj);
            _db.SaveChanges();

            return userObj;
        }
    }
}