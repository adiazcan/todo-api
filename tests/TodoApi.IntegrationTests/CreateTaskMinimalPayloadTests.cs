using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TodoApi.Functions.Functions;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.IntegrationTests;

public sealed class CreateTaskMinimalPayloadTests
{
    [Fact]
    public async Task RunAsync_should_accept_the_minimal_published_request_body()
    {
        var todoTaskService = new CountingTodoTaskService();
        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateJsonRequest("""
            {
              "text": "Pick up groceries",
              "listId": "list-456"
            }
            """);

        var response = await function.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        todoTaskService.InvocationCount.Should().Be(1);

        var payload = await TestHttpData.ReadJsonAsync(response.Body);
        payload.GetProperty("success").GetBoolean().Should().BeTrue();
        payload.GetProperty("data").GetProperty("taskId").GetString().Should().Be("task-123");
    }

    [Fact]
    public async Task RunAsync_should_reject_unknown_request_fields()
    {
        var todoTaskService = new CountingTodoTaskService();
        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateJsonRequest("""
            {
              "text": "Pick up groceries",
              "listId": "list-456",
              "priority": "high"
            }
            """);

        var response = await function.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        todoTaskService.InvocationCount.Should().Be(0);

        var payload = await TestHttpData.ReadJsonAsync(response.Body);
        payload.GetProperty("error").GetProperty("code").GetString().Should().Be("validation.invalid_request");
    }

    [Fact]
    public async Task RunAsync_should_dispatch_exactly_one_create_operation_for_a_successful_request()
    {
        var todoTaskService = new CountingTodoTaskService();
        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateJsonRequest("""
            {
              "text": "Pick up groceries",
              "listId": "list-456"
            }
            """);

        var response = await function.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        todoTaskService.InvocationCount.Should().Be(1);
    }

    private static CreateTaskFunction CreateFunction(CountingTodoTaskService todoTaskService)
    {
        return new CreateTaskFunction(
            todoTaskService,
            new CreateTaskRequestValidator(new TodoErrorMapper()),
            new TodoErrorMapper(),
            new ApiResponseFactory(),
            NullLogger<CreateTaskFunction>.Instance);
    }

    private sealed class CountingTodoTaskService : ITodoTaskService
    {
        public int InvocationCount { get; private set; }

        public Task<CreatedTodoTask> CreateTaskAsync(NormalizedTaskCommand command, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(new CreatedTodoTask(
                Id: "task-123",
                Title: command.Title,
                ListId: command.ListId,
                ListName: "Errands",
                CreatedAtUtc: DateTimeOffset.Parse("2026-03-14T10:15:30Z")));
        }
    }

    private static class TestHttpData
    {
        public static HttpRequestData CreateJsonRequest(string json)
        {
            var context = new Mock<FunctionContext>();
            var responseStream = new MemoryStream();

            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(item => item.StatusCode, HttpStatusCode.OK);
            response.SetupGet(item => item.Headers).Returns(new HttpHeadersCollection());
            response.SetupGet(item => item.Body).Returns(responseStream);
            response.SetupGet(item => item.Cookies).Returns(Mock.Of<HttpCookies>());

            var body = new MemoryStream(Encoding.UTF8.GetBytes(json));

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