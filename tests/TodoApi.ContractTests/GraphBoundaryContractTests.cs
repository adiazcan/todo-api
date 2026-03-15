using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.ContractTests;

public sealed class GraphBoundaryContractTests
{
    [Fact]
    public async Task CreateTaskAsync_should_translate_the_published_request_and_response_shape_at_the_graph_boundary()
    {
        var createdAtUtc = DateTimeOffset.Parse("2026-03-14T15:05:22Z");
        var handler = new RecordingGraphHandler(createdAtUtc);
        var service = new TodoTaskService(CreateGraphClient(handler), new TodoErrorMapper(), NullLogger<TodoTaskService>.Instance);

        var createdTask = await service.CreateTaskAsync(
            new NormalizedTaskCommand("Buy milk", "list-456", DateTimeOffset.Parse("2026-03-14T15:05:00Z")),
            CancellationToken.None);

        createdTask.Id.Should().Be("task-123");
        createdTask.Title.Should().Be("Buy milk");
        createdTask.ListId.Should().Be("list-456");
        createdTask.ListName.Should().Be("Errands");
        createdTask.CreatedAtUtc.Should().Be(createdAtUtc);

        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].Path.Should().Be("/v1.0/me/todo/lists/list-456");
        handler.Requests[1].Method.Should().Be(HttpMethod.Post);
        handler.Requests[1].Path.Should().Be("/v1.0/me/todo/lists/list-456/tasks");

        using var requestBody = JsonDocument.Parse(handler.Requests[1].Body!);
        requestBody.RootElement.GetProperty("title").GetString().Should().Be("Buy milk");
        requestBody.RootElement.TryGetProperty("text", out _).Should().BeFalse();
        requestBody.RootElement.TryGetProperty("listId", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CreateTaskAsync_should_fall_back_to_the_normalized_title_when_graph_omits_it_from_the_create_response()
    {
        var handler = new RecordingGraphHandler(createdAtUtc: null, createdTaskTitle: null);
        var service = new TodoTaskService(CreateGraphClient(handler), new TodoErrorMapper(), NullLogger<TodoTaskService>.Instance);

        var createdTask = await service.CreateTaskAsync(
            new NormalizedTaskCommand("Call the dentist", "list-456", DateTimeOffset.Parse("2026-03-14T16:00:00Z")),
            CancellationToken.None);

        createdTask.Title.Should().Be("Call the dentist");
        createdTask.CreatedAtUtc.Should().BeNull();
    }

    private static GraphServiceClient CreateGraphClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
        };

        var authenticationProvider = new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider());
        return new GraphServiceClient(httpClient, authenticationProvider);
    }

    private sealed class StaticAccessTokenProvider : IAccessTokenProvider
    {
        public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);

        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("test-access-token");
        }
    }

    private sealed class RecordingGraphHandler : HttpMessageHandler
    {
        private readonly DateTimeOffset? _createdAtUtc;
        private readonly string? _createdTaskTitle;

        public RecordingGraphHandler(DateTimeOffset? createdAtUtc, string? createdTaskTitle = "Buy milk")
        {
            _createdAtUtc = createdAtUtc;
            _createdTaskTitle = createdTaskTitle;
        }

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.AbsolutePath ?? string.Empty, body));

            return (request.Method.Method, request.RequestUri?.AbsolutePath) switch
            {
                ("GET", "/v1.0/me/todo/lists/list-456") => CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "id": "list-456",
                      "displayName": "Errands"
                    }
                    """),
                ("POST", "/v1.0/me/todo/lists/list-456/tasks") => CreateJsonResponse(HttpStatusCode.Created, CreateTaskJson()),
                _ => CreateJsonResponse(HttpStatusCode.NotFound, "{}")
            };
        }

        private string CreateTaskJson()
        {
            var createdDateTime = _createdAtUtc is null ? "null" : $"\"{_createdAtUtc:O}\"";
            var title = _createdTaskTitle is null ? "null" : JsonSerializer.Serialize(_createdTaskTitle, typeof(string));

            return $$"""
            {
              "id": "task-123",
              "title": {{title}},
              "createdDateTime": {{createdDateTime}}
            }
            """;
        }

        private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path, string? Body);
}