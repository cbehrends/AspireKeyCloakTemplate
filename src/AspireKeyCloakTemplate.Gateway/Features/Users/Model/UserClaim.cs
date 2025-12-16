namespace AspireKeyCloakTemplate.Gateway.Features.Users.Model;

internal sealed class UserClaim
{
    public required string Type { get; init; }
    public required string Value { get; init; }
}
