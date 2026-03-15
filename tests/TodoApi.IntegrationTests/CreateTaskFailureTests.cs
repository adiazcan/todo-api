using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TodoApi.Functions.Contracts;
using TodoApi.Functions.Functions;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.IntegrationTests;

public sealed class CreateTaskFailureTests
{
    [Fact]
    public async Task RunAsync_should_return_bad_request_when_text_is_blank()
    {
        var todoTaskService = new StubTodoTaskService();
        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateRequest(new CreateTaskRequest
        {
            Text = "   ",
            ListId = "list-456"
        });

        var response = await function.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        todoTaskService.Invocations.Should().Be(0);

        var payload = await TestHttpData.ReadJsonAsync(response.Body);
        payload.GetProperty("success").GetBoolean().Should().BeFalse();
        payload.GetProperty("error").GetProperty("code").GetString().Should().Be("validation.task_text_required");
    }

    [Fact]
    public async Task RunAsync_should_return_bad_request_when_list_id_is_blank()
    {
        var todoTaskService = new StubTodoTaskService();
        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateRequest(new CreateTaskRequest
        {
            Text = "Buy milk",
            ListId = "   "
        });

        var response = await function.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        todoTaskService.Invocations.Should().Be(0);

        var payload = await TestHttpData.ReadJsonAsync(response.Body);
        payload.GetProperty("error").GetProperty("code").GetString().Should().Be("validation.list_id_required");
    }

    [Fact]
    public async Task RunAsync_should_return_unauthorized_when_the_configured_account_requires_reauthorization()
    {
        var todoTaskService = new StubTodoTaskService
        {
            Exception = new UnauthorizedAccessException("reauthorization required")
        };

        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateRequest(new CreateTaskRequest
        {
            Text = "Buy milk",
            ListId = "list-456"
        });

        var response = await function.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var payload = await TestHttpData.ReadJsonAsync(response.Body);
        payload.GetProperty("error").GetProperty("code").GetString().Should().Be("auth.unauthorized");
        payload.GetProperty("error").GetProperty("retryable").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_should_return_bad_gateway_for_non_transient_graph_failures()
    {
        var todoTaskService = new StubTodoTaskService
        {
            Exception = new InvalidOperationException("graph returned an invalid response")
        };

        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateRequest(new CreateTaskRequest
        {
            Text = "Buy milk",
            ListId = "list-456"
        });

        var response = await function.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

        var payload = await TestHttpData.ReadJsonAsync(response.Body);
        payload.GetProperty("error").GetProperty("code").GetString().Should().Be("todo.upstream_failure");
        payload.GetProperty("error").GetProperty("retryable").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_should_return_service_unavailable_for_transient_graph_failures()
    {
        var todoTaskService = new StubTodoTaskService
        {
            Exception = new TimeoutException("graph timed out")
        };

        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateRequest(new CreateTaskRequest
        {
            Text = "Buy milk",
            ListId = "list-456"
        });

        var response = await function.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var payload = await TestHttpData.ReadJsonAsync(response.Body);
        payload.GetProperty("error").GetProperty("code").GetString().Should().Be("todo.temporarily_unavailable");
        payload.GetProperty("error").GetProperty("retryable").GetBoolean().Should().BeTrue();
    }

    private static CreateTaskFunction CreateFunction(StubTodoTaskService todoTaskService)
    {
        return new CreateTaskFunction(
            todoTaskService,
            new CreateTaskRequestValidator(new TodoErrorMapper()),
            new TodoErrorMapper(),
            new ApiResponseFactory(),
            NullLogger<CreateTaskFunction>.Instance);
    }

    private sealed class StubTodoTaskService : ITodoTaskService
    {
        public Exception? Exception { get; init; }

        public int Invocations { get; private set; }

        public Task<CreatedTodoTask> CreateTaskAsync(NormalizedTaskCommand command, CancellationToken cancellationToken = default)
        {
            Invocations++;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(new CreatedTodoTask(
                Id: "task-123",
                Title: command.Title,
                ListId: command.ListId,
                ListName: "Errands",
                CreatedAtUtc: DateTimeOffset.UtcNow));
        }
    }

    private static class TestHttpData
    {
        public static HttpRequestData CreateRequest(CreateTaskRequest payload)
        {
            var context = new Mock<FunctionContext>();
            var responseStream = new MemoryStream();

            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(item => item.StatusCode, HttpStatusCode.OK);
            response.SetupGet(item => item.Headers).Returns(new HttpHeadersCollection());
            response.SetupGet(item => item.Body).Returns(responseStream);
            response.SetupGet(item => item.Cookies).Returns(Mock.Of<HttpCookies>());

            var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));

            var request = new Mock<HttpRequestData>(context.Object);
            request.SetupGet(item => item.Body).Returns(body);
            request.SetupGet(item => item.Headers).Returns(new HttpHeadersCollection());
            request.SetupGet(item => item.Cookies).Returns(Array.Empty<IHttpCookie>());
            request.SetupGet(item => item.Url).Returns(new Uri("https://localhost/api/tasks"));
            request.SetupGet(item => item.Identities).Returns(Array.Empty<ClaimsIdentity>());
            request.SetupGet(item => item.Method).Returns("POST");
            request.Setup(item => item.CreateResponse()).Returns(response.Object);

            return request.Object;
        }

        public static async Task<JsonElement> ReadJsonAsync(Stream body)
        {
            body.Position = 0;
            using var document = await JsonDocument.ParseAsync(body);
            return document.RootElement.Clone();
        }
    }
}