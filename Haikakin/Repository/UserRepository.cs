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
using System.Security.Cryptography;
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

        public AuthenticateResponse Authenticate(AuthenticationModel model, LoginTypeEnum loginType, string ipAddress)
        {
            var encryptPassword = Encrypt.HMACSHA256(model.Password, _appSettings.UserSecret);

            User user = null;
            if (!string.IsNullOrEmpty(model.Email))
            {
                user = _db.Users.SingleOrDefault(x => x.Email == model.Email && x.Password == encryptPassword && x.LoginType == loginType);

            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                user = _db.Users.SingleOrDefault(x => x.PhoneNumber == model.PhoneNumber && x.Password == encryptPassword && x.LoginType == loginType);
            }

            if (user == null)
            {
                return null;
            }

            var jwtToken = generateJwtToken(user);
            var refreshToken = generateRefreshToken(ipAddress);

            user.RefreshTokens.Add(refreshToken);
            if (user.RefreshTokens.Count >= 2)
            {
                user.RefreshTokens.RemoveRange(0, user.RefreshTokens.Count - 1);
            }
            user.LastLoginTime = DateTime.UtcNow;
            _db.Update(user);
            _db.SaveChanges();

            return new AuthenticateResponse(user, jwtToken, refreshToken.Token);
        }

        public AuthenticateResponse AuthenticateThird(string userEmail, LoginTypeEnum loginType, string ipAddress)
        {
            var user = _db.Users.SingleOrDefault(x => x.Email == userEmail && x.LoginType == loginType);

            if (user == null)
            {
                return null;
            }

            var jwtToken = generateJwtToken(user);
            var refreshToken = generateRefreshToken(ipAddress);

            user.RefreshTokens.Add(refreshToken);
            if (user.RefreshTokens.Count >= 2)
            { 
                user.RefreshTokens.RemoveRange(0, user.RefreshTokens.Count - 1);
            }
            user.LastLoginTime = DateTime.UtcNow;
            _db.Update(user);
            _db.SaveChanges();

            return new AuthenticateResponse(user, jwtToken, refreshToken.Token);
        }

        public AuthenticateResponse RefreshToken(string token, string ipAddress)
        {
            var user = _db.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));

            // return null if no user found with token
            if (user == null) return null;

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            // return null if token is no longer active
            if (!refreshToken.IsActive) return null;

            // replace old refresh token with a new one and save
            var newRefreshToken = generateRefreshToken(ipAddress);
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            if (user.RefreshTokens.Count >= 2)
            {
                user.RefreshTokens.RemoveRange(0, user.RefreshTokens.Count - 1);
            }
            user.RefreshTokens.Add(newRefreshToken);
            _db.Update(user);
            _db.SaveChanges();

            // generate new jwt
            var jwtToken = generateJwtToken(user);

            return new AuthenticateResponse(user, jwtToken, newRefreshToken.Token);
        }

        public bool RevokeToken(string token, string ipAddress)
        {
            var user = _db.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));

            // return false if no user found with token
            if (user == null) return false;

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            // return false if token is not active
            if (!refreshToken.IsActive) return false;

            // revoke token and save
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            _db.Update(user);
            _db.SaveChanges();

            return true;
        }

        public bool IsUniqueUser(string email)
        {
            var user = _db.Users.SingleOrDefault(x => x.Email == email);

            if (user == null) return true;

            return false;
        }

        public User Register(RegisterModel model)
        {
            var encryptPassword = Encrypt.HMACSHA256(model.Password, _appSettings.UserSecret);

            User userObj = new User()
            {
                Username = model.Username,
                Email = model.Email,
                EmailVerity = false,
                Password = encryptPassword,
                Role = "User",
                PhoneNumber = model.PhoneNumber,
                PhoneNumberVerity = true,
                CreateTime = DateTime.UtcNow,
                LoginType = LoginTypeEnum.Normal,
            };

            _db.Users.Add(userObj);
            _db.SaveChanges();
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

        public User GetUser(int id)
        {
            return _db.Users.SingleOrDefault(x => x.UserId == id);
        }

        public bool UpdateUser(User user)
        {
            _db.Users.Update(user);
            return Save();
        }

        public bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }

        private string generateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.JwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name,$"{user.UserId}"),
                    new Claim(ClaimTypes.Role,user.Role),
                    new Claim(ClaimTypes.Email,user.Email)
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken generateRefreshToken(string ipAddress)
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[64];
                rngCryptoServiceProvider.GetBytes(randomBytes);
                return new RefreshToken
                {
                    Token = Convert.ToBase64String(randomBytes),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    CreatedByIp = ipAddress
                };
            }
        }
    }
}