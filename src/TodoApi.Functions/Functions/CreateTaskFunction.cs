using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using TodoApi.Functions.Contracts;
using TodoApi.Functions.Services;

namespace TodoApi.Functions.Functions;

public sealed class CreateTaskFunction
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ITodoTaskService _todoTaskService;
    private readonly ApiResponseFactory _apiResponseFactory;

    public CreateTaskFunction(ITodoTaskService todoTaskService, ApiResponseFactory apiResponseFactory)
    {
        _todoTaskService = todoTaskService;
        _apiResponseFactory = apiResponseFactory;
    }

    [Function("CreateTask")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var payload = await JsonSerializer.DeserializeAsync<CreateTaskRequest>(
            request.Body,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false);

        if (payload is null)
        {
            throw new InvalidOperationException("The task creation request body is required.");
        }

        var command = new NormalizedTaskCommand(
            Title: payload.Text.Trim(),
            ListId: payload.ListId.Trim(),
            RequestedAtUtc: DateTimeOffset.UtcNow);

        var createdTask = await _todoTaskService.CreateTaskAsync(command, cancellationToken).ConfigureAwait(false);
        var response = request.CreateResponse(HttpStatusCode.Created);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        var payloadResponse = _apiResponseFactory.CreateTaskCreated(
            createdTask.Id,
            createdTask.Title,
            createdTask.ListId,
            createdTask.ListName,
            createdTask.CreatedAtUtc);

        await JsonSerializer.SerializeAsync(response.Body, payloadResponse, SerializerOptions, cancellationToken).ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        response.Body.Position = 0;
        return response;
    }
}