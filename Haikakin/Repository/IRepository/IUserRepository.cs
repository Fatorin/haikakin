using Haikakin.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.User;

namespace Haikakin.Repository.IRepository
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string email);
        AuthenticateResponse Authenticate(AuthenticationModel model, LoginTypeEnum loginType, string ipAddress);
        AuthenticateResponse AuthenticateAdmin(AuthenticationModel model, LoginTypeEnum loginType, string ipAddress);
        AuthenticateResponse AuthenticateThird(string userEmail, LoginTypeEnum loginType, string ipAddress);
        AuthenticateResponse RefreshToken(string token, string ipAddress);
        bool RevokeToken(string token, string ipAddress);
        User Register(RegisterModel model);
        User RegisterThird(string username, string email, LoginTypeEnum loginType);
        User GetUser(int id);
        ICollection<User> GetUsers();
        bool UpdateUser(User user);
        bool Save();
    }
}
