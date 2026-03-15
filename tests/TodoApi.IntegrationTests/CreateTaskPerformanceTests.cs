using System.Diagnostics;
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

public sealed class CreateTaskPerformanceTests
{
    [Fact]
    public async Task RunAsync_should_complete_a_successful_request_within_the_five_second_target()
    {
        var todoTaskService = new DelayedTodoTaskService(TimeSpan.FromMilliseconds(25));
        var function = CreateFunction(todoTaskService);
        var request = TestHttpData.CreateRequest(new CreateTaskRequest
        {
            Text = "Buy milk",
            ListId = "list-456"
        });

        var stopwatch = Stopwatch.StartNew();
        var response = await function.RunAsync(request, CancellationToken.None);
        stopwatch.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
        todoTaskService.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_should_complete_a_100_request_burst_within_the_five_second_target()
    {
        var todoTaskService = new DelayedTodoTaskService(TimeSpan.FromMilliseconds(25));
        var function = CreateFunction(todoTaskService);

        var stopwatch = Stopwatch.StartNew();
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 100).Select(index => function.RunAsync(
                TestHttpData.CreateRequest(new CreateTaskRequest
                {
                    Text = $"Buy milk {index}",
                    ListId = "list-456"
                }),
                CancellationToken.None)));
        stopwatch.Stop();

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Created);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
        todoTaskService.InvocationCount.Should().Be(100);
    }

    private static CreateTaskFunction CreateFunction(DelayedTodoTaskService todoTaskService)
    {
        return new CreateTaskFunction(
            TestSerializerOptions.Create(),
            todoTaskService,
            new CreateTaskRequestValidator(new TodoErrorMapper()),
            new TodoErrorMapper(),
            new ApiResponseFactory(),
            NullLogger<CreateTaskFunction>.Instance);
    }

    private sealed class DelayedTodoTaskService : ITodoTaskService
    {
        private readonly TimeSpan _delay;
        private int _invocationCount;

        public DelayedTodoTaskService(TimeSpan delay)
        {
            _delay = delay;
        }

        public int InvocationCount => _invocationCount;

        public async Task<CreatedTodoTask> CreateTaskAsync(NormalizedTaskCommand command, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _invocationCount);
            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);

            return new CreatedTodoTask(
                Id: $"task-{command.Title}",
                Title: command.Title,
                ListId: command.ListId,
                ListName: "Errands",
                CreatedAtUtc: DateTimeOffset.UtcNow);
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
    }
}