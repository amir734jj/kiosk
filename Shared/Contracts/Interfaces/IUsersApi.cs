using Refit;

namespace Shared.Contracts.Interfaces;

[Headers("Authorization: Bearer")]
public interface IUsersApi
{
    [Get("/api/users")]
    Task<List<UserDto>> GetAllAsync();

    [Post("/api/users/{id}/activate")]
    Task ActivateAsync(int id);

    [Post("/api/users/{id}/deactivate")]
    Task DeactivateAsync(int id);

    [Delete("/api/users/{id}")]
    Task DeleteAsync(int id);

    [Post("/api/users/{id}/make-admin")]
    Task MakeAdminAsync(int id);

    [Post("/api/users/{id}/make-user")]
    Task MakeUserAsync(int id);
}
