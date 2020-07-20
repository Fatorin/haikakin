using Haikakin.Data;
using Haikakin.Models;
using Haikakin.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Repository
{
    public class SmsRepository : ISmsRepository
    {
        private readonly ApplicationDbContext _db;

        public SmsRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public ICollection<SmsModel> GetSmsModels()
        {
            return _db.SmsModels.OrderBy(u => u.Id).ToList();
        }
        public SmsModel GetSmsModel(string phoneNumber)
        {
            return _db.SmsModels.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
        }

        public bool IsUniqueSmsModel(string phoneNumber)
        {
            var user = _db.SmsModels.SingleOrDefault(x => x.PhoneNumber == phoneNumber);

            if (user == null) return true;

            return false;
        }

        public bool CreateSmsModel(SmsModel smsModel)
        {
            _db.Add(smsModel);
            return Save();
        }
        public bool UpdateSmsModel(SmsModel smsModel)
        {
            _db.Update(smsModel);
            return Save();
        }

        public bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }

    }
}
