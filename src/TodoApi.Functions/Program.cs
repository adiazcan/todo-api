using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using TodoApi.Functions.Auth;
using TodoApi.Functions.Options;
using TodoApi.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddOptions();
        services.AddSingleton<IConfigureOptions<GraphOptions>, GraphOptionsSetup>();
        services.AddSingleton<IValidateOptions<GraphOptions>, GraphOptionsSetup>();
        services.AddOptions<GraphOptions>().ValidateOnStart();

        services.AddTransient<GraphRetryHandler>();
        services.AddHttpClient(GraphServiceClientFactory.HttpClientName, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<GraphOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        })
        .AddHttpMessageHandler<GraphRetryHandler>();

        services.AddSingleton<IGraphAccessTokenProvider, MsalGraphAccessTokenProvider>();
        services.AddSingleton<GraphServiceClientFactory>();
        services.AddSingleton<GraphServiceClient>(serviceProvider =>
            serviceProvider.GetRequiredService<GraphServiceClientFactory>().CreateClient());
        services.AddSingleton<ITodoTaskService, TodoTaskService>();
        services.AddSingleton<TodoErrorMapper>();
        services.AddSingleton<ApiResponseFactory>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

await host.RunAsync();