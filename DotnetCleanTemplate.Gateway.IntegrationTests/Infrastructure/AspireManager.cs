using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace DotnetCleanTemplate.Gateway.IntegrationTests.Infrastructure;

/// <summary>
/// Manages the lifecycle of an Aspire distributed application and its dependencies for integration testing.
/// Implements <see cref="IAsyncLifetime"/> to handle asynchronous initialization and disposal.
/// </summary>
public class AspireManager : IAsyncLifetime
{
    /// <summary>
    /// Gets the PlaywrightManager instance used for browser-based testing.
    /// </summary>
    internal PlaywrightManager PlaywrightManager { get; } = new();

    /// <summary>
    /// Gets the configured Aspire DistributedApplication instance.
    /// </summary>
    internal DistributedApplication? App { get; private set; }

    /// <summary>
    /// Configures and starts an Aspire DistributedApplication instance.
    /// </summary>
    /// <typeparam name="TEntryPoint">The entry point class of the application.</typeparam>
    /// <param name="args">Optional command-line arguments for the application.</param>
    /// <param name="configureBuilder">Optional action to configure the application builder.</param>
    /// <returns>The configured and started <see cref="DistributedApplication"/> instance.</returns>
    public async Task<DistributedApplication> ConfigureAsync<TEntryPoint>(
        string[]? args = null,
        Action<IDistributedApplicationTestingBuilder>? configureBuilder = null) where TEntryPoint : class
    {
        // Return the existing application instance if already configured
        if (App is not null) return App;

        // Create and configure the application builder
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TEntryPoint>(
            args: args ?? [],
            configureBuilder: static (options, _) =>
            {
                options.DisableDashboard = false; // Enable the Aspire dashboard
            });

        // Set configuration for unsecured transport
        builder.Configuration["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "true";

        // Apply additional builder configurations if provided
        configureBuilder?.Invoke(builder);

        // Build and start the application
        App = await builder.BuildAsync();
        await App.StartAsync();

        return App;
    }

    /// <summary>
    /// Initializes the AspireManager and its dependencies asynchronously.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Initialize the PlaywrightManager
        await PlaywrightManager.InitializeAsync();
    }

    /// <summary>
    /// Disposes of the AspireManager and its dependencies asynchronously.
    /// </summary>
    public async Task DisposeAsync()
    {
        // Dispose of the PlaywrightManager
        await PlaywrightManager.DisposeAsync();

        // Dispose of the Aspire application if it exists
        await (App?.DisposeAsync() ?? ValueTask.CompletedTask);
    }
}
