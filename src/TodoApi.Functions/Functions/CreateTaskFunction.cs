using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TodoApi.Functions.Contracts;
using TodoApi.Functions.Services;

namespace TodoApi.Functions.Functions;

public sealed class CreateTaskFunction
{
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ITodoTaskService _todoTaskService;
    private readonly CreateTaskRequestValidator _requestValidator;
    private readonly TodoErrorMapper _todoErrorMapper;
    private readonly ApiResponseFactory _apiResponseFactory;
    private readonly ILogger<CreateTaskFunction> _logger;

    public CreateTaskFunction(
        IOptions<JsonSerializerOptions> serializerOptions,
        ITodoTaskService todoTaskService,
        CreateTaskRequestValidator requestValidator,
        TodoErrorMapper todoErrorMapper,
        ApiResponseFactory apiResponseFactory,
        ILogger<CreateTaskFunction> logger)
    {
        _serializerOptions = serializerOptions.Value;
        _todoTaskService = todoTaskService;
        _requestValidator = requestValidator;
        _todoErrorMapper = todoErrorMapper;
        _apiResponseFactory = apiResponseFactory;
        _logger = logger;
    }

    [Function("CreateTask")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestedAtUtc = DateTimeOffset.UtcNow;
            CreateTaskRequest? payload;

            try
            {
                payload = await JsonSerializer.DeserializeAsync<CreateTaskRequest>(
                    request.Body,
                    _serializerOptions,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException exception)
            {
                var invalidRequest = _todoErrorMapper.InvalidRequest();
                _logger.LogWarning(exception, "Task creation request body could not be parsed.");
                return await CreateErrorResponseAsync(request, invalidRequest, cancellationToken).ConfigureAwait(false);
            }

            var validationResult = _requestValidator.Validate(payload, requestedAtUtc);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Task creation request failed validation with code {ErrorCode}.",
                    validationResult.Failure!.Error.Code);

                return await CreateErrorResponseAsync(request, validationResult.Failure, cancellationToken).ConfigureAwait(false);
            }

            var command = validationResult.Command!;
            var createdTask = await _todoTaskService.CreateTaskAsync(command, cancellationToken).ConfigureAwait(false);
            var response = request.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var payloadResponse = _apiResponseFactory.CreateTaskCreated(
                createdTask.Id,
                createdTask.Title,
                createdTask.ListId,
                createdTask.ListName,
                createdTask.CreatedAtUtc);

            await JsonSerializer.SerializeAsync(response.Body, payloadResponse, _serializerOptions, cancellationToken).ConfigureAwait(false);
            await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (response.Body.CanSeek)
            {
                response.Body.Position = 0;
            }

            _logger.LogInformation(
                "Created task {TaskId} in list {ListId}.",
                createdTask.Id,
                createdTask.ListId);

            return response;
        }
        catch (TodoTaskOperationException exception)
        {
            return await HandleFailureAsync(
                    request,
                    new MappedFailure(exception.StatusCode, exception.Error),
                    exception,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return await HandleFailureAsync(
                    request,
                    _todoErrorMapper.Map(exception),
                    exception,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<HttpResponseData> HandleFailureAsync(
        HttpRequestData request,
        MappedFailure failure,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogFailure(failure, exception);
        return await CreateErrorResponseAsync(request, failure, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData request,
        MappedFailure failure,
        CancellationToken cancellationToken)
    {
        var response = request.CreateResponse(failure.StatusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        var payload = _apiResponseFactory.CreateError(failure.Error);
        await JsonSerializer.SerializeAsync(response.Body, payload, _serializerOptions, cancellationToken).ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        if (response.Body.CanSeek)
        {
            response.Body.Position = 0;
        }

        return response;
    }

    private void LogFailure(MappedFailure failure, Exception exception)
    {
        if ((int)failure.StatusCode >= 500 && !failure.Error.Retryable)
        {
            _logger.LogError(
                exception,
                "Task creation failed with status {StatusCode} and code {ErrorCode}.",
                (int)failure.StatusCode,
                failure.Error.Code);
            return;
        }

        _logger.LogWarning(
            exception,
            "Task creation failed with status {StatusCode} and code {ErrorCode}.",
            (int)failure.StatusCode,
            failure.Error.Code);
    }
}