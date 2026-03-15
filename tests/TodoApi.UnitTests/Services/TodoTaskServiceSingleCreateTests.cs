using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.UnitTests.Services;

public sealed class TodoTaskServiceSingleCreateTests
{
    [Fact]
    public async Task CreateTaskAsync_should_issue_exactly_one_create_request_after_list_validation_succeeds()
    {
        var handler = new RecordingHttpMessageHandler(
        [
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""
                {
                  "id": "list-123",
                  "displayName": "Errands"
                }
                """)
            },
            new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent("""
                {
                  "id": "task-456",
                  "title": "Buy milk"
                }
                """)
            }
        ]);

        var service = new TodoTaskService(CreateGraphClient(handler), new TodoErrorMapper(), NullLogger<TodoTaskService>.Instance);

        _ = await service.CreateTaskAsync(new NormalizedTaskCommand(
            Title: "Buy milk",
            ListId: "list-123",
            RequestedAtUtc: DateTimeOffset.UtcNow));

        handler.Requests.Should().HaveCount(2);
        handler.Requests.Count(request => request.Method == HttpMethod.Get).Should().Be(1);
        handler.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
    }

    [Fact]
    public async Task CreateTaskAsync_should_not_issue_a_create_request_when_list_validation_fails()
    {
        var handler = new RecordingHttpMessageHandler(
        [
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent("""
                {
                  "error": {
                    "code": "ErrorItemNotFound",
                    "message": "The list was not found."
                  }
                }
                """)
            }
        ]);

        var service = new TodoTaskService(CreateGraphClient(handler), new TodoErrorMapper(), NullLogger<TodoTaskService>.Instance);

        var action = async () => await service.CreateTaskAsync(new NormalizedTaskCommand(
            Title: "Buy milk",
            ListId: "missing-list",
            RequestedAtUtc: DateTimeOffset.UtcNow));

        await action.Should().ThrowAsync<TodoTaskOperationException>();

        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
    }

    private static GraphServiceClient CreateGraphClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
        };

        return new GraphServiceClient(
            httpClient,
            new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider("graph.microsoft.com")));
    }

    private static StringContent JsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private sealed class StaticAccessTokenProvider : IAccessTokenProvider
    {
        public StaticAccessTokenProvider(string allowedHost)
        {
            AllowedHostsValidator = new AllowedHostsValidator([allowedHost]);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; }

        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("test-token");
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public RecordingHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured for the outgoing Microsoft Graph request.");
            }

            var response = _responses.Dequeue();
            response.RequestMessage = request;
            response.Headers.Date ??= DateTimeOffset.UtcNow;
            response.Content.Headers.ContentType ??= new MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }
    }
}