using Api.Data.Entities;
using EfCoreRepository;

namespace Api.Data.Profiles;

public class AnnouncementProfile : EntityProfile<Announcement>
{
    public AnnouncementProfile()
    {
        MapAll();
    }
}
