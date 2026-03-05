using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using reviews.Services;

namespace reviews.Middleware;

public class SessionBootstrapMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionBootstrapMiddleware> _logger;

    public SessionBootstrapMiddleware(RequestDelegate next, ILogger<SessionBootstrapMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionBootstrapService sessionBootstrapService)
    {
        var endpoint = context.GetEndpoint();
        var isAnonymousEndpoint = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null;

        if (!isAnonymousEndpoint && context.User.Identity?.IsAuthenticated == true)
        {
            var result = await sessionBootstrapService.EnsureInitializedAsync(context.User);
            if (!result.Success)
            {
                _logger.LogWarning(
                    "Session bootstrap failed. Path={Path} Reason={Reason} Email={Email} Role={Role}",
                    context.Request.Path,
                    result.Reason,
                    result.Email ?? "<unknown>",
                    result.Role ?? "<unknown>");
            }
            else if (result.SessionRebuilt)
            {
                _logger.LogInformation(
                    "Session bootstrap rebuilt session. Path={Path} Reason={Reason} Email={Email} Role={Role}",
                    context.Request.Path,
                    result.Reason,
                    result.Email ?? "<unknown>",
                    result.Role ?? "<unknown>");
            }
        }

        await _next(context);
    }
}
