using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Extensions;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Api.Logging;

public class UsernameEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity is not { IsAuthenticated: true })
            return;

        var username = httpContext.User.TryGetUsername();
        if (!string.IsNullOrEmpty(username))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("User", username));

        var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!string.IsNullOrEmpty(userId))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserId", userId));
    }
}
