using System.Diagnostics;
using Microsoft.Playwright;

namespace AspireKeyCloakTemplate.IntegrationTests.Core;

/// <summary>
///     Configure Playwright for interacting with the browser in tests.
/// </summary>
public class PlaywrightManager : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private static bool IsDebugging => Debugger.IsAttached;
    private static bool IsHeadless => IsDebugging is false;

    internal IBrowser Browser { get; set; } = null!;

    public async Task InitializeAsync()
    {
        Assertions.SetDefaultExpectTimeout(10_000);

        _playwright = await Playwright.CreateAsync();

        var options = new BrowserTypeLaunchOptions
        {
            Headless = IsHeadless
        };

        Browser = await _playwright.Chromium.LaunchAsync(options).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();

        _playwright?.Dispose();
    }
}
