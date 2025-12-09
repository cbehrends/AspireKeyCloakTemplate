using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace AspireKeyCloakTemplate.Gateway.IntegrationTests.Infrastructure;

/// <summary>
/// Manages the lifecycle of a Playwright browser instance for integration testing.
/// Implements <see cref="IAsyncLifetime"/> to handle asynchronous initialization and disposal.
/// </summary>
public class PlaywrightManager : IAsyncLifetime
{
    /// <summary>
    /// Determines if the debugger is attached, which affects the browser's headless mode.
    /// </summary>
    private static bool IsDebugging => Debugger.IsAttached;

    /// <summary>
    /// Indicates whether the browser should run in headless mode.
    /// Defaults to headless unless debugging.
    /// </summary>
    private static bool IsHeadless => !IsDebugging;

    /// <summary>
    /// Holds the Playwright instance used to create and manage browser contexts.
    /// </summary>
    private IPlaywright? _playwright;

    /// <summary>
    /// Gets or sets the browser instance used for testing.
    /// </summary>
    internal IBrowser Browser { get; set; } = null!;

    /// <summary>
    /// Initializes the PlaywrightManager by creating a Playwright instance and launching a browser.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Set the default timeout for Playwright assertions
        Assertions.SetDefaultExpectTimeout(10_000);

        // Create a new Playwright instance
        _playwright = await Playwright.CreateAsync();

        // Configure browser launch options
        var options = new BrowserTypeLaunchOptions
        {
            Headless = IsHeadless // Run in headless mode unless debugging
        };

        // Launch the browser with the specified options
        Browser = await _playwright.Chromium.LaunchAsync(options).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes of the PlaywrightManager by closing the browser and releasing resources.
    /// </summary>
    public async Task DisposeAsync()
    {
        // Close the browser instance
        await Browser.CloseAsync();

        // Dispose of the Playwright instance
        _playwright?.Dispose();
    }
}
