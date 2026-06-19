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
        if (string.IsNullOrEmpty(username))
            return;

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("User", username));
    }
}
