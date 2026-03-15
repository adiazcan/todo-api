namespace TodoApi.Functions.Services;

public sealed record CreatedTodoTask(
    string Id,
    string Title,
    string ListId,
    string ListName,
    DateTimeOffset? CreatedAtUtc);