using Blazored.LocalStorage;

namespace UI.Services;

public sealed class AuthService(ILocalStorageService storage)
{
    private const string TokenKey = "auth_token";
    private const string RoleKey = "auth_role";
    private const string UserIdKey = "auth_userid";
    private const string OriginalTokenKey = "auth_original_token";
    private const string OriginalRoleKey = "auth_original_role";
    private const string OriginalUserIdKey = "auth_original_userid";

    public string? Token { get; private set; }
    public string? Role { get; private set; }
    public int? UserId { get; private set; }
    public bool IsImpersonating { get; private set; }

    public bool IsAuthenticated => Token is not null;
    public bool IsAdmin => Role == "Admin";
    public bool IsRealAdmin => IsImpersonating || IsAdmin;

    public event Action? AuthStateChanged;

    public async Task InitAsync()
    {
        Token = await storage.GetItemAsync<string>(TokenKey);
        Role = await storage.GetItemAsync<string>(RoleKey);
        UserId = await storage.GetItemAsync<int?>(UserIdKey);
        IsImpersonating = await storage.GetItemAsync<string>(OriginalTokenKey) is not null;
    }

    public async Task SetTokenAsync(string token, string role, int userId)
    {
        Token = token;
        Role = role;
        UserId = userId;
        await storage.SetItemAsync(TokenKey, token);
        await storage.SetItemAsync(RoleKey, role);
        await storage.SetItemAsync(UserIdKey, userId);
        AuthStateChanged?.Invoke();
    }

    public async Task StartImpersonatingAsync(string token, string role, int userId)
    {
        await storage.SetItemAsync(OriginalTokenKey, Token);
        await storage.SetItemAsync(OriginalRoleKey, Role);
        await storage.SetItemAsync(OriginalUserIdKey, UserId);

        Token = token;
        Role = role;
        UserId = userId;
        IsImpersonating = true;

        await storage.SetItemAsync(TokenKey, token);
        await storage.SetItemAsync(RoleKey, role);
        await storage.SetItemAsync(UserIdKey, userId);
        AuthStateChanged?.Invoke();
    }

    public async Task StopImpersonatingAsync()
    {
        Token = await storage.GetItemAsync<string>(OriginalTokenKey);
        Role = await storage.GetItemAsync<string>(OriginalRoleKey);
        UserId = await storage.GetItemAsync<int?>(OriginalUserIdKey);
        IsImpersonating = false;

        await storage.SetItemAsync(TokenKey, Token);
        await storage.SetItemAsync(RoleKey, Role);
        await storage.SetItemAsync(UserIdKey, UserId);

        await storage.RemoveItemAsync(OriginalTokenKey);
        await storage.RemoveItemAsync(OriginalRoleKey);
        await storage.RemoveItemAsync(OriginalUserIdKey);
        AuthStateChanged?.Invoke();
    }

    public async Task LogoutAsync()
    {
        Token = null;
        Role = null;
        UserId = null;
        IsImpersonating = false;
        await storage.RemoveItemAsync(TokenKey);
        await storage.RemoveItemAsync(RoleKey);
        await storage.RemoveItemAsync(UserIdKey);
        await storage.RemoveItemAsync(OriginalTokenKey);
        await storage.RemoveItemAsync(OriginalRoleKey);
        await storage.RemoveItemAsync(OriginalUserIdKey);
        AuthStateChanged?.Invoke();
    }
}
