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

public sealed class CreateTaskSuccessTests
{
    [Fact]
    public async Task RunAsync_should_return_created_payload_for_a_successful_task_creation()
    {
        var todoTaskService = new CapturingTodoTaskService
        {
            Result = new CreatedTodoTask(
                Id: "task-123",
                Title: "Buy milk",
                ListId: "list-456",
                ListName: "Errands",
                CreatedAtUtc: DateTimeOffset.Parse("2026-03-14T10:15:30Z"))
        };

            var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateRequest(new CreateTaskRequest
        {
            Text = "Buy milk",
            ListId = "list-456"
        });

        var response = await function.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await TestHttpData.ReadJsonAsync(response.Body);
        payload.GetProperty("success").GetBoolean().Should().BeTrue();
        payload.GetProperty("data").GetProperty("taskId").GetString().Should().Be("task-123");
        payload.GetProperty("data").GetProperty("listId").GetString().Should().Be("list-456");
        payload.GetProperty("data").GetProperty("listName").GetString().Should().Be("Errands");
    }

    [Fact]
    public async Task RunAsync_should_trim_text_and_list_id_before_dispatching_to_the_service()
    {
        var todoTaskService = new CapturingTodoTaskService();
        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateRequest(new CreateTaskRequest
        {
            Text = "  Call the dentist  ",
            ListId = "  list-123  "
        });

        _ = await function.RunAsync(request, CancellationToken.None);

        todoTaskService.LastCommand.Should().NotBeNull();
        todoTaskService.LastCommand!.Title.Should().Be("Call the dentist");
        todoTaskService.LastCommand.ListId.Should().Be("list-123");
    }

    [Fact]
    public async Task RunAsync_should_preserve_multiline_and_punctuation_when_normalizing_text()
    {
        var todoTaskService = new CapturingTodoTaskService();
        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateRequest(new CreateTaskRequest
        {
            Text = "  Review agenda:\n- budget?\n- launch!  ",
            ListId = "list-789"
        });

        _ = await function.RunAsync(request, CancellationToken.None);

        todoTaskService.LastCommand.Should().NotBeNull();
        todoTaskService.LastCommand!.Title.Should().Be("Review agenda:\n- budget?\n- launch!");
    }

    private static CreateTaskFunction CreateFunction(CapturingTodoTaskService todoTaskService)
    {
        return new CreateTaskFunction(
            TestSerializerOptions.Create(),
            todoTaskService,
            new CreateTaskRequestValidator(new TodoErrorMapper()),
            new TodoErrorMapper(),
            new ApiResponseFactory(),
            NullLogger<CreateTaskFunction>.Instance);
    }

    private sealed class CapturingTodoTaskService : ITodoTaskService
    {
        public NormalizedTaskCommand? LastCommand { get; private set; }

        public CreatedTodoTask Result { get; set; } = new(
            Id: "task-123",
            Title: "Buy milk",
            ListId: "list-456",
            ListName: "Errands",
            CreatedAtUtc: DateTimeOffset.Parse("2026-03-14T10:15:30Z"));

        public Task<CreatedTodoTask> CreateTaskAsync(NormalizedTaskCommand command, CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(Result);
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