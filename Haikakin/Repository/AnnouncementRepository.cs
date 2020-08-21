using Haikakin.Data;
using Haikakin.Models;
using Haikakin.Models.AnnouncementModel;
using Haikakin.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Haikakin.Models.OrderModel;
using static Haikakin.Repository.IRepository.IAnnouncementRepository;

namespace Haikakin.Repository
{
    public class AnnouncementRepository : IAnnouncementRepository
    {
        private readonly ApplicationDbContext _db;

        public AnnouncementRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public bool AnnouncementExists(int id)
        {
            bool value = _db.Announcements.Any(u => u.AnnouncementId == id);
            return value;
        }

        public ICollection<Announcement> GetAnnouncements(QueryMode queryMode)
        {
            if (queryMode == QueryMode.User)
            {
                var datas = _db.Announcements.Where(u => u.IsActive == true).ToList();
                datas.Reverse();
                return datas;
            }
            else
            {
                var datas = _db.Announcements.OrderBy(u => u.AnnouncementId).ToList();
                datas.Reverse();
                return datas;
            }
        }

        public Announcement GetAnnouncement(int id)
        {            
            return _db.Announcements.SingleOrDefault(ann => ann.AnnouncementId == id);
        }

        public bool CreateAnnouncement(Announcement announcement)
        {
            _db.Announcements.Add(announcement);
            return Save();
        }

        public bool UpdateAnnouncement(Announcement announcement)
        {
            _db.Announcements.Update(announcement);
            return Save();
        }

        public bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }
    }
}
