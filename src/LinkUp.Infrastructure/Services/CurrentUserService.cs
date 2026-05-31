using System.Security.Claims;
using LinkUp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LinkUp.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContext;

    public CurrentUserService(IHttpContextAccessor httpContext)
        => _httpContext = httpContext;

    public Guid UserId
    {
        get
        {
            var claim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                     ?? _httpContext.HttpContext?.User?.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
        }
    }

    public string UserEmail
        => _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
        ?? _httpContext.HttpContext?.User?.FindFirst("email")?.Value
        ?? string.Empty;
}
