using Haikakin.Models.AnnouncementModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Repository.IRepository
{
    public interface IAnnouncementRepository
    {
        ICollection<Announcement> GetAnnouncements(QueryMode queryMode);

        Announcement GetAnnouncement(int id);

        bool AnnouncementExists(int id);

        bool CreateAnnouncement(Announcement announcement);

        bool UpdateAnnouncement(Announcement announcement);

        bool Save();

        public enum QueryMode { Admin, User }
    }
}
