using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace TodoApi.Functions.Services;

public sealed class TodoTaskService : ITodoTaskService
{
    private readonly GraphServiceClient _graphServiceClient;

    public TodoTaskService(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
    }

    public async Task<CreatedTodoTask> CreateTaskAsync(NormalizedTaskCommand command, CancellationToken cancellationToken = default)
    {
        var targetList = await _graphServiceClient.Me.Todo.Lists[command.ListId]
            .GetAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (targetList is null)
        {
            throw new InvalidOperationException("Microsoft Graph did not return the requested Microsoft To Do list.");
        }

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

        return new CreatedTodoTask(
            Id: taskId,
            Title: string.IsNullOrWhiteSpace(createdTask.Title) ? command.Title : createdTask.Title,
            ListId: string.IsNullOrWhiteSpace(targetList.Id) ? command.ListId : targetList.Id,
            ListName: listName,
            CreatedAtUtc: createdTask.CreatedDateTime);
    }
}