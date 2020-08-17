using Haikakin.Data;
using Haikakin.Models;
using Haikakin.Models.AnnouncementModel;
using Haikakin.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.Order;
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
            if (queryMode == QueryMode.Admin)
            {
                return _db.Announcements.Where(u => u.IsActive == true).Reverse().ToList();
            }
            else
            {
                return _db.Announcements.OrderBy(u=>u.AnnouncementId).Reverse().ToList();
            }
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
