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
        User Authenticate(string email, string password, LoginTypeEnum lognType);
        User AuthenticateThird(string email, LoginTypeEnum lognType);
        User Register(string username, string email, string password);
        User RegisterThird(string username, string email, LoginTypeEnum loginType);
    }
}
