namespace Backend.Demo.DependencyInjection;

public static class BackendDemoInitializationExtensions {
    public static async Task InitializeBackendDemoAsync(this IServiceProvider serviceProvider) {
        await using var scope = serviceProvider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>();
        await initializer.InitializeAsync();
    }
}
