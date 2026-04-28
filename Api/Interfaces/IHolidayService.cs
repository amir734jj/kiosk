using Shared.Contracts;

namespace Api.Interfaces;

public interface IHolidayService
{
    Task<string?> GetTodayHolidayAsync();
}
