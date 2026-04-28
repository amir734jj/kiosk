using Refit;
using Shared.Contracts;
using Shared.Contracts.Interfaces;

namespace UI.Services;

public sealed class ApiService(
    IAuthApi authApi,
    IOfficesApi officesApi,
    IAnnouncementsApi announcementsApi,
    IUsersApi usersApi,
    IPublicApi publicApi,
    IGlobalConfigApi globalConfigApi,
    AuthService auth)
{
    // --- Auth ---
    public async Task<string?> LoginAsync(string username, string password)
    {
        try
        {
            var response = await authApi.LoginAsync(new LoginRequest(username, password));
            await auth.SetTokenAsync(response.Token, response.Role, response.UserId);
            return null;
        }
        catch (ApiException ex) when ((int)ex.StatusCode == 403)
        {
            return "Account is pending activation by an administrator.";
        }
        catch (ApiException)
        {
            return "Invalid username or password.";
        }
    }

    public async Task<(bool Success, string? Error, bool IsActive)> RegisterAsync(string username, string password, string passwordConfirm)
    {
        try
        {
            var result = await authApi.RegisterAsync(new RegisterRequest(username, password, passwordConfirm));
            return (true, null, result.IsActive);
        }
        catch (ApiException ex)
        {
            var error = ex.Content ?? "Registration failed.";
            return (false, error, false);
        }
    }

    public async Task<MeResponse?> GetMeAsync()
    {
        try { return await authApi.MeAsync(); }
        catch { return null; }
    }

    public async Task<string?> ImpersonateAsync(int userId)
    {
        try
        {
            var response = await authApi.ImpersonateAsync(userId);
            await auth.StartImpersonatingAsync(response.Token, response.Role, response.UserId);
            return null;
        }
        catch (ApiException ex)
        {
            return $"Impersonation failed: {ex.Content}";
        }
    }

    public async Task StopImpersonatingAsync()
    {
        await auth.StopImpersonatingAsync();
    }

    // --- Offices ---
    public Task<List<OfficeDto>> GetOfficesAsync() => officesApi.GetAllAsync();

    public async Task<OfficeDto?> GetMyOfficeAsync()
    {
        try { return await officesApi.GetMyAsync(); }
        catch { return null; }
    }

    public async Task<(bool Ok, string? Error)> CreateOfficeAsync(CreateOfficeRequest req)
    {
        try { await officesApi.CreateAsync(req); return (true, null); }
        catch (ApiException ex) { return (false, ex.Content); }
    }

    public async Task<bool> UpdateOfficeAsync(int id, UpdateOfficeRequest req)
    {
        try { await officesApi.UpdateAsync(id, req); return true; }
        catch { return false; }
    }

    public async Task<bool> UpdateMyOfficeAsync(UpdateOfficeRequest req)
    {
        try { await officesApi.UpdateMyAsync(req); return true; }
        catch { return false; }
    }

    public Task DeleteOfficeAsync(int id) => officesApi.DeleteAsync(id);

    public Task AssignUserToOfficeAsync(int officeId, int userId) => officesApi.AssignUserAsync(officeId, userId);
    public Task UnassignUserFromOfficeAsync(int userId) => officesApi.UnassignUserAsync(userId);

    // --- Announcements ---
    public Task<List<AnnouncementDto>> GetAnnouncementsAsync() => announcementsApi.GetAllAsync();

    public async Task<(bool Ok, string? Error)> CreateAnnouncementAsync(CreateAnnouncementRequest req)
    {
        try { await announcementsApi.CreateAsync(req); return (true, null); }
        catch (ApiException ex) { return (false, ex.Content); }
    }

    public async Task<bool> UpdateAnnouncementAsync(int id, UpdateAnnouncementRequest req)
    {
        try { await announcementsApi.UpdateAsync(id, req); return true; }
        catch { return false; }
    }

    public Task DeleteAnnouncementAsync(int id) => announcementsApi.DeleteAsync(id);

    // --- Users ---
    public Task<List<UserDto>> GetUsersAsync() => usersApi.GetAllAsync();
    public Task ActivateUserAsync(int id) => usersApi.ActivateAsync(id);
    public Task DeactivateUserAsync(int id) => usersApi.DeactivateAsync(id);
    public Task DeleteUserAsync(int id) => usersApi.DeleteAsync(id);
    public Task MakeAdminAsync(int id) => usersApi.MakeAdminAsync(id);
    public Task MakeUserAsync(int id) => usersApi.MakeUserAsync(id);

    // --- Public ---
    public Task<PublicDisplayDto> GetPublicDisplayAsync() => publicApi.GetDisplayAsync();

    // --- Global Config ---
    public Task<GlobalConfigModel> GetGlobalConfigAsync() => globalConfigApi.GetAsync();
    public Task SaveGlobalConfigAsync(GlobalConfigModel config) => globalConfigApi.SaveAsync(config);
}
