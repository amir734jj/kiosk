using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public int GetUserId()
        {
            var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? throw new InvalidOperationException("User ID claim missing.");
            return int.Parse(value);
        }

        public string? TryGetUsername()
        {
            return user.FindFirstValue(JwtRegisteredClaimNames.UniqueName);
        }
    }
}
