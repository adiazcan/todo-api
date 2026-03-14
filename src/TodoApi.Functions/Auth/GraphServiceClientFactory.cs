using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using TodoApi.Functions.Options;

namespace TodoApi.Functions.Auth;

public sealed class GraphServiceClientFactory
{
    public const string HttpClientName = "MicrosoftGraph";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GraphOptions _options;
    private readonly IGraphAccessTokenProvider _tokenProvider;

    public GraphServiceClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<GraphOptions> options,
        IGraphAccessTokenProvider tokenProvider)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _tokenProvider = tokenProvider;
    }

    public GraphServiceClient CreateClient()
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        var authenticationProvider = new BaseBearerTokenAuthenticationProvider(
            new GraphAccessTokenAdapter(_tokenProvider, new Uri(_options.BaseUrl, UriKind.Absolute).Host));

        return new GraphServiceClient(httpClient, authenticationProvider);
    }

    private sealed class GraphAccessTokenAdapter : IAccessTokenProvider
    {
        private readonly IGraphAccessTokenProvider _tokenProvider;

        public GraphAccessTokenAdapter(IGraphAccessTokenProvider tokenProvider, string allowedHost)
        {
            _tokenProvider = tokenProvider;
            AllowedHostsValidator = new AllowedHostsValidator([allowedHost]);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; }

        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            return _tokenProvider.GetAccessTokenAsync(cancellationToken);
        }
    }
}

public sealed class GraphRetryHandler : DelegatingHandler
{
    private static readonly HttpStatusCode[] TransientStatusCodes =
    [
        HttpStatusCode.RequestTimeout,
        (HttpStatusCode)429,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    ];

    private readonly ILogger<GraphRetryHandler> _logger;
    private readonly GraphOptions _options;

    public GraphRetryHandler(IOptions<GraphOptions> options, ILogger<GraphRetryHandler> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= _options.MaxRetryAttempts; attempt++)
        {
            using var clonedRequest = await CloneRequestAsync(request, cancellationToken).ConfigureAwait(false);

            try
            {
                var response = await base.SendAsync(clonedRequest, cancellationToken).ConfigureAwait(false);
                if (!IsTransient(response.StatusCode) || attempt == _options.MaxRetryAttempts)
                {
                    return response;
                }

                var delay = GetRetryDelay(response, attempt);
                _logger.LogWarning(
                    "Microsoft Graph request failed with transient status code {StatusCode}. Retrying in {Delay}.",
                    (int)response.StatusCode,
                    delay);

                response.Dispose();
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (IsTransient(exception, cancellationToken) && attempt < _options.MaxRetryAttempts)
            {
                var delay = GetRetryDelay(response: null, attempt);
                _logger.LogWarning(exception, "Microsoft Graph request failed transiently. Retrying in {Delay}.", delay);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("Retry pipeline exhausted without returning a response.");
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return Array.IndexOf(TransientStatusCodes, statusCode) >= 0;
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken)
    {
        return exception switch
        {
            OperationCanceledException when !cancellationToken.IsCancellationRequested => true,
            HttpRequestException => true,
            TimeoutException => true,
            _ => false
        };
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage? response, int attempt)
    {
        var retryAfter = response?.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var retryDelay = date - DateTimeOffset.UtcNow;
            if (retryDelay > TimeSpan.Zero)
            {
                return retryDelay;
            }
        }

        return TimeSpan.FromMilliseconds(Math.Min(1000 * Math.Pow(2, attempt), 4000));
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}