using Api.Data.Entities;
using EfCoreRepository;

namespace Api.Data.Profiles;

public class OfficeProfile : EntityProfile<Office>
{
    public OfficeProfile()
    {
        Map(x => x.UnitNumber);
        Map(x => x.Name);
        Map(x => x.Names);
        Map(x => x.PhoneNumber);
        Map(x => x.Note);
    }
}
