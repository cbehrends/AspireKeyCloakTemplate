namespace AspireKeyCloakTemplate.Gateway.Features.Users.Model;

internal sealed class User
{
    public bool IsAuthenticated { get; init; }
    public string? Name { get; init; }
    public IEnumerable<UserClaim> Claims { get; init; } = [];
}