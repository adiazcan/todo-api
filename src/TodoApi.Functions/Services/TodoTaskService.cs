using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace TodoApi.Functions.Services;

public sealed class TodoTaskService : ITodoTaskService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly TodoErrorMapper _todoErrorMapper;
    private readonly ILogger<TodoTaskService> _logger;

    public TodoTaskService(
        GraphServiceClient graphServiceClient,
        TodoErrorMapper todoErrorMapper,
        ILogger<TodoTaskService> logger)
    {
        _graphServiceClient = graphServiceClient;
        _todoErrorMapper = todoErrorMapper;
        _logger = logger;
    }

    public async Task<CreatedTodoTask> CreateTaskAsync(NormalizedTaskCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resolving Microsoft To Do list {ListId} before task creation.", command.ListId);

            var targetList = await _graphServiceClient.Me.Todo.Lists[command.ListId]
                .GetAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (targetList is null)
            {
                throw new InvalidOperationException("Microsoft Graph did not return the requested Microsoft To Do list.");
            }

            _logger.LogInformation("Creating Microsoft To Do task in list {ListId}.", command.ListId);

            var createdTask = await _graphServiceClient.Me.Todo.Lists[command.ListId].Tasks
                .PostAsync(
                    new TodoTask
                    {
                        Title = command.Title
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (createdTask is null)
            {
                throw new InvalidOperationException("Microsoft Graph did not return the created Microsoft To Do task.");
            }

            var taskId = createdTask.Id;
            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw new InvalidOperationException("Microsoft Graph did not return a task identifier.");
            }

            var listName = targetList.DisplayName;
            if (string.IsNullOrWhiteSpace(listName))
            {
                throw new InvalidOperationException("Microsoft Graph did not return the target list name.");
            }

            _logger.LogInformation("Created Microsoft To Do task {TaskId} in list {ListId}.", taskId, command.ListId);

            return new CreatedTodoTask(
                Id: taskId,
                Title: string.IsNullOrWhiteSpace(createdTask.Title) ? command.Title : createdTask.Title,
                ListId: string.IsNullOrWhiteSpace(targetList.Id) ? command.ListId : targetList.Id,
                ListName: listName,
                CreatedAtUtc: createdTask.CreatedDateTime);
        }
        catch (Exception exception) when (TryTranslateFailure(exception, command.ListId, out var translatedException))
        {
            throw translatedException;
        }
    }

    private bool TryTranslateFailure(Exception exception, string listId, out TodoTaskOperationException translatedException)
    {
        if (exception is TodoTaskOperationException mappedException)
        {
            translatedException = mappedException;
            return true;
        }

        var mappedFailure = _todoErrorMapper.Map(exception, operation: "create_task", listId: listId);
        var logLevel = mappedFailure.StatusCode switch
        {
            HttpStatusCode.BadGateway => LogLevel.Error,
            _ => LogLevel.Warning
        };

        _logger.Log(logLevel,
            exception,
            "Task creation failed for list {ListId} with status {StatusCode} and code {ErrorCode}.",
            listId,
            (int)mappedFailure.StatusCode,
            mappedFailure.Error.Code);

        translatedException = new TodoTaskOperationException(mappedFailure.StatusCode, mappedFailure.Error, exception);
        return true;
    }
}