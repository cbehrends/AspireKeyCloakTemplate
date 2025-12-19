using System.Security.Claims;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

namespace AspireKeyCloakTemplate.BFF.Features.Users.Queries.GetCurrentUser;

/// <summary>
/// Handler for GetCurrentUserQuery
/// </summary>
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserInfo>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetCurrentUserQueryHandler> _logger;

    public GetCurrentUserQueryHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetCurrentUserQueryHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<UserInfo> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var name = user.FindFirstValue("name") ?? user.Identity.Name;
            var email = user.FindFirstValue("email");
            var claims = user.Claims.Select(c => new UserClaim(c.Type, c.Value));

            _logger.LogInformation("Retrieved current user information for {UserName}", name);

            return Task.FromResult(new UserInfo(
                true,
                name,
                email,
                claims));
        }

        _logger.LogInformation("User is not authenticated");

        return Task.FromResult(new UserInfo(
            false,
            null,
            null,
            Enumerable.Empty<UserClaim>()));
    }
}

