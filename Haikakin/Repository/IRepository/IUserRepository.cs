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
        User Authenticate(string userEmail, string userPhone, string password, LoginTypeEnum loginType);
        User AuthenticateThird(string email, LoginTypeEnum lognType);
        User Register(RegisterModel model);
        User RegisterThird(string username, string email, LoginTypeEnum loginType);
        User GetUser(int id);
        bool UpdateUser(User user);
        bool Save();
    }
}
