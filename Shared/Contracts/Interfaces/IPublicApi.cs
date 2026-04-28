using Refit;

namespace Shared.Contracts.Interfaces;

public interface IPublicApi
{
    [Get("/api/public/display")]
    Task<PublicDisplayDto> GetDisplayAsync();
}
