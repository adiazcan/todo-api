using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.UnitTests.Services;

public sealed class TodoTaskServiceTests
{
    [Fact]
    public async Task CreateTaskAsync_should_validate_the_requested_list_before_creating_the_task()
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
                  "title": "Buy milk",
                  "createdDateTime": "2026-03-14T10:15:30Z"
                }
                """)
            }
        ]);

        var service = new TodoTaskService(CreateGraphClient(handler));

        var result = await service.CreateTaskAsync(new NormalizedTaskCommand(
            Title: "Buy milk",
            ListId: "list-123",
            RequestedAtUtc: DateTimeOffset.Parse("2026-03-14T10:15:00Z")));

        result.Id.Should().Be("task-456");
        result.Title.Should().Be("Buy milk");
        result.ListId.Should().Be("list-123");
        result.ListName.Should().Be("Errands");
        result.CreatedAtUtc.Should().Be(DateTimeOffset.Parse("2026-03-14T10:15:30Z"));

        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri!.AbsolutePath.Should().EndWith("/me/todo/lists/list-123");
        handler.Requests[1].Method.Should().Be(HttpMethod.Post);
        handler.Requests[1].RequestUri!.AbsolutePath.Should().EndWith("/me/todo/lists/list-123/tasks");

        using var postedTask = JsonDocument.Parse(handler.RequestBodies[1]);
        postedTask.RootElement.GetProperty("title").GetString().Should().Be("Buy milk");
    }

    [Fact]
    public async Task CreateTaskAsync_should_preserve_multiline_and_punctuation_in_the_created_graph_payload()
    {
        const string title = "Review agenda:\n- budget?\n- launch!";

        var handler = new RecordingHttpMessageHandler(
        [
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""
                {
                  "id": "list-999",
                  "displayName": "Work"
                }
                """)
            },
            new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent($$"""
                {
                  "id": "task-999",
                  "title": {{JsonSerializer.Serialize(title)}}
                }
                """)
            }
        ]);

        var service = new TodoTaskService(CreateGraphClient(handler));

        _ = await service.CreateTaskAsync(new NormalizedTaskCommand(
            Title: title,
            ListId: "list-999",
            RequestedAtUtc: DateTimeOffset.UtcNow));

        using var postedTask = JsonDocument.Parse(handler.RequestBodies[1]);
        postedTask.RootElement.GetProperty("title").GetString().Should().Be(title);
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

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured for the outgoing Microsoft Graph request.");
            }

            var response = _responses.Dequeue();
            response.RequestMessage = request;
            response.Headers.Date ??= DateTimeOffset.UtcNow;
            response.Content.Headers.ContentType ??= new MediaTypeHeaderValue("application/json");
            return response;
        }
    }
}