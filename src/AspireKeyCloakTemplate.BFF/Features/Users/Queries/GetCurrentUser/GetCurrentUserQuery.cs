using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

namespace AspireKeyCloakTemplate.BFF.Features.Users.Queries.GetCurrentUser;

/// <summary>
///     Query to get the current authenticated user information
/// </summary>
public record GetCurrentUserQuery : IRequest<UserInfo>;

/// <summary>
///     Response containing user information
/// </summary>
public record UserInfo(
    bool IsAuthenticated,
    string? Name,
    string? Email,
    IEnumerable<UserClaim> Claims);

public record UserClaim(string Type, string Value);
