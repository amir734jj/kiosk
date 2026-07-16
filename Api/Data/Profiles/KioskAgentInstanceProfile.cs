using Api.Data.Entities;
using EfCoreRepository;

namespace Api.Data.Profiles;

public class KioskAgentInstanceProfile : EntityProfile<KioskAgentInstance>
{
    public KioskAgentInstanceProfile()
    {
        MapAll();
    }
}
