namespace TodoApi.Functions.Services;

public interface ITodoTaskService
{
    Task<CreatedTodoTask> CreateTaskAsync(NormalizedTaskCommand command, CancellationToken cancellationToken = default);
}