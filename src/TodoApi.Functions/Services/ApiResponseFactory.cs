using TodoApi.Functions.Contracts;

namespace TodoApi.Functions.Services;

public sealed class ApiResponseFactory
{
    public CreateTaskSuccessResponse CreateTaskCreated(
        string taskId,
        string title,
        string listId,
        string listName,
        DateTimeOffset? createdAtUtc = null)
    {
        return new CreateTaskSuccessResponse
        {
            Data = new CreateTaskSuccessData
            {
                TaskId = taskId,
                Title = title,
                ListId = listId,
                ListName = listName,
                CreatedAtUtc = createdAtUtc
            }
        };
    }

    public ErrorResponse CreateError(ApiErrorDescriptor error)
    {
        return new ErrorResponse
        {
            Error = new ErrorDetails
            {
                Code = error.Code,
                Message = error.Message,
                Retryable = error.Retryable,
                Details = error.Details
            }
        };
    }
}

public sealed record ApiErrorDescriptor(
    string Code,
    string Message,
    bool Retryable,
    IReadOnlyDictionary<string, object?>? Details = null);