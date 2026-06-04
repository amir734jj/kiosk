using Api.Data.Entities;
using EfCoreRepository;

namespace Api.Data.Profiles;

public class AdvertisementProfile : EntityProfile<Advertisement>
{
    public AdvertisementProfile()
    {
        MapAll();
    }
}
