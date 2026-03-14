using Microsoft.Identity.Client;

namespace TodoApi.Functions.Services;

public sealed class TodoErrorMapper
{
    public ApiErrorDescriptor TaskTextRequired(IReadOnlyDictionary<string, object?>? details = null)
    {
        return new ApiErrorDescriptor(
            Code: "validation.task_text_required",
            Message: "Task text is required.",
            Retryable: false,
            Details: details);
    }

    public ApiErrorDescriptor ListIdRequired(IReadOnlyDictionary<string, object?>? details = null)
    {
        return new ApiErrorDescriptor(
            Code: "validation.list_id_required",
            Message: "Task list ID is required.",
            Retryable: false,
            Details: details);
    }

    public ApiErrorDescriptor Unauthorized(string message = "The request is not authorized for this endpoint.")
    {
        return new ApiErrorDescriptor(
            Code: "auth.unauthorized",
            Message: message,
            Retryable: false);
    }

    public ApiErrorDescriptor UpstreamFailure(
        string message = "Microsoft To Do could not create the task.",
        IReadOnlyDictionary<string, object?>? details = null)
    {
        return new ApiErrorDescriptor(
            Code: "todo.upstream_failure",
            Message: message,
            Retryable: false,
            Details: details);
    }

    public ApiErrorDescriptor TemporarilyUnavailable(
        string message = "Microsoft To Do is temporarily unavailable. Retry later.",
        IReadOnlyDictionary<string, object?>? details = null)
    {
        return new ApiErrorDescriptor(
            Code: "todo.temporarily_unavailable",
            Message: message,
            Retryable: true,
            Details: details);
    }

    public ApiErrorDescriptor Map(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => Unauthorized(),
            MsalUiRequiredException => Unauthorized("The configured Microsoft account requires interactive reauthorization."),
            MsalServiceException serviceException when IsTransientMsal(serviceException) =>
                TemporarilyUnavailable(details: CreateDetails(serviceException.ErrorCode, serviceException.StatusCode)),
            MsalServiceException serviceException =>
                UpstreamFailure(details: CreateDetails(serviceException.ErrorCode, serviceException.StatusCode)),
            HttpRequestException => TemporarilyUnavailable(),
            OperationCanceledException => TemporarilyUnavailable(),
            TimeoutException => TemporarilyUnavailable(),
            _ => UpstreamFailure()
        };
    }

    private static bool IsTransientMsal(MsalServiceException exception)
    {
        return exception.StatusCode is 408 or 429 or 500 or 502 or 503 or 504
            || string.Equals(exception.ErrorCode, "temporarily_unavailable", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, object?> CreateDetails(string? errorCode, int statusCode)
    {
        return new Dictionary<string, object?>
        {
            ["provider"] = "microsoft-graph",
            ["errorCode"] = errorCode,
            ["statusCode"] = statusCode
        };
    }
}