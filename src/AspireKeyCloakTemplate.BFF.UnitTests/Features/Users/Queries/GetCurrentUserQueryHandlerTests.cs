using System.Security.Claims;
using AspireKeyCloakTemplate.BFF.Features.Users.Queries.GetCurrentUser;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AspireKeyCloakTemplate.BFF.UnitTests.Features.Users.Queries;

public class GetCurrentUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAuthenticatedUserInfo_WhenUserIsAuthenticated()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("name", "Test User"),
            new Claim("email", "test@example.com"),
            new Claim("role", "admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        var httpContextAccessorSub = Substitute.For<IHttpContextAccessor>();
        httpContextAccessorSub.HttpContext.Returns(httpContext);

        var loggerSub = Substitute.For<ILogger<GetCurrentUserQueryHandler>>();
        var handler = new GetCurrentUserQueryHandler(httpContextAccessorSub, loggerSub);

        // Act
        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsAuthenticated);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("test@example.com", result.Email);
        Assert.Contains(result.Claims, c => c.Type == "role" && c.Value == "admin");
    }

    [Fact]
    public async Task Handle_ReturnsUnauthenticatedUserInfo_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        var httpContextAccessorSub = Substitute.For<IHttpContextAccessor>();
        httpContextAccessorSub.HttpContext.Returns(httpContext);

        var loggerSub = Substitute.For<ILogger<GetCurrentUserQueryHandler>>();
        var handler = new GetCurrentUserQueryHandler(httpContextAccessorSub, loggerSub);

        // Act
        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        Assert.False(result.IsAuthenticated);
        Assert.Null(result.Name);
        Assert.Null(result.Email);
        Assert.Empty(result.Claims);
    }

    [Fact]
    public async Task Handle_ReturnsAuthenticatedUserInfo_WithMissingNameAndEmail()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("role", "user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        var httpContextAccessorSub = Substitute.For<IHttpContextAccessor>();
        httpContextAccessorSub.HttpContext.Returns(httpContext);

        var loggerSub = Substitute.For<ILogger<GetCurrentUserQueryHandler>>();
        var handler = new GetCurrentUserQueryHandler(httpContextAccessorSub, loggerSub);

        // Act
        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsAuthenticated);
        Assert.Null(result.Name); // No name claim or Identity.Name
        Assert.Null(result.Email); // No email claim
        Assert.Contains(result.Claims, c => c.Type == "role" && c.Value == "user");
    }
}
