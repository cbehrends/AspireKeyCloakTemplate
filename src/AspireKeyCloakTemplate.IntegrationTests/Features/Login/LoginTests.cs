using AspireKeyCloakTemplate.IntegrationTests.Core;
using Microsoft.Playwright;
using Projects;
using Shouldly;

namespace AspireKeyCloakTemplate.IntegrationTests.Features.Login;

public class LoginTests(AspireManager aspireManager) : PlaywrightTestBase(aspireManager)
{
    [Fact(Skip = "Times out when running in GitHub Actions")]
    public async Task Login_Logout_Flow_Works()
    {
        // Ensure the AppHost is started and endpoints are available
        await ConfigureAsync<AppHost>();
        await InteractWithPageAsync("bff", async page =>
        {
            // Wait for the Login link and click it
            var response = await page.GotoAsync("/");
            response.ShouldNotBeNull();
            response.Status.ShouldBe(200);
            var loginLink = await page.WaitForSelectorAsync("a:text('Login')");
            loginLink.ShouldNotBeNull();

            await loginLink.ClickAsync();

            // Wait for the Keycloak login page
            await page.WaitForURLAsync(url => url.ToString().Contains("/realms/sandbox/login-actions/authenticate"),
                new PageWaitForURLOptions { Timeout = 60000 });

            // Fill in credentials
            await page.FillAsync("input[name='username'], input[name='email'], input[placeholder*='Username']",
                "testuser");
            await page.FillAsync("input[type='password']", "password123");

            // Click Sign In
            var signInButton = await page.QuerySelectorAsync("button:text('Sign In')");
            signInButton.ShouldNotBeNull();
            await signInButton.ClickAsync();

            // Wait for redirect back to BFF and check for Logout
            var bffEndpoint = AspireManager.App?.GetEndpoint("bff").ToString();
            await page.WaitForURLAsync(url => url.ToString().StartsWith(bffEndpoint!),
                new PageWaitForURLOptions { Timeout = 60000 });
            var logoutButton = await page.WaitForSelectorAsync("button:text('Logout')");
            logoutButton.ShouldNotBeNull();

            // Click Logout
            await logoutButton.ClickAsync();

            // Wait for Login link to reappear
            var loginLinkAfter = await page.WaitForSelectorAsync("a:text('Login')");
            loginLinkAfter.ShouldNotBeNull();
        });
    }
}
