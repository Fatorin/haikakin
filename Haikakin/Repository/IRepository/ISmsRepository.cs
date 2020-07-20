using Haikakin.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.User;

namespace Haikakin.Repository.IRepository
{
    public interface ISmsRepository
    {
        ICollection<SmsModel> GetSmsModels();
        SmsModel GetSmsModel(string phoneNumber);
        bool IsUniqueSmsModel(string phoneNumber);
        bool CreateSmsModel(SmsModel smsModel);
        bool UpdateSmsModel(SmsModel smsModel);
        bool Save();
    }
}
