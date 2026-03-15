using System.Net;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions;

namespace TodoApi.Functions.Services;

public sealed class TodoErrorMapper
{
    public MappedFailure TaskTextRequired(IReadOnlyDictionary<string, object?>? details = null)
    {
        return new MappedFailure(
            HttpStatusCode.BadRequest,
            new ApiErrorDescriptor(
                Code: "validation.task_text_required",
                Message: "Task text is required.",
                Retryable: false,
                Details: details));
    }

    public MappedFailure ListIdRequired(IReadOnlyDictionary<string, object?>? details = null)
    {
        return new MappedFailure(
            HttpStatusCode.BadRequest,
            new ApiErrorDescriptor(
                Code: "validation.list_id_required",
                Message: "Task list ID is required.",
                Retryable: false,
                Details: details));
    }

    public MappedFailure InvalidRequest(
        string message = "Request body must be valid JSON containing text and listId.",
        IReadOnlyDictionary<string, object?>? details = null)
    {
        return new MappedFailure(
            HttpStatusCode.BadRequest,
            new ApiErrorDescriptor(
                Code: "validation.invalid_request",
                Message: message,
                Retryable: false,
                Details: details));
    }

    public MappedFailure Unauthorized(
        string message = "The request is not authorized for this endpoint.",
        IReadOnlyDictionary<string, object?>? details = null)
    {
        return new MappedFailure(
            HttpStatusCode.Unauthorized,
            new ApiErrorDescriptor(
                Code: "auth.unauthorized",
                Message: message,
                Retryable: false,
                Details: details));
    }

    public MappedFailure UpstreamFailure(
        string message = "Microsoft To Do could not create the task.",
        IReadOnlyDictionary<string, object?>? details = null)
    {
        return new MappedFailure(
            HttpStatusCode.BadGateway,
            new ApiErrorDescriptor(
                Code: "todo.upstream_failure",
                Message: message,
                Retryable: false,
                Details: details));
    }

    public MappedFailure TemporarilyUnavailable(
        string message = "Microsoft To Do is temporarily unavailable. Retry later.",
        IReadOnlyDictionary<string, object?>? details = null)
    {
        return new MappedFailure(
            HttpStatusCode.ServiceUnavailable,
            new ApiErrorDescriptor(
                Code: "todo.temporarily_unavailable",
                Message: message,
                Retryable: true,
                Details: details));
    }

    public MappedFailure Map(Exception exception, string? operation = null, string? listId = null)
    {
        return exception switch
        {
            TodoTaskOperationException mappedException => new MappedFailure(mappedException.StatusCode, mappedException.Error),
            UnauthorizedAccessException => Unauthorized(details: CreateDetails(provider: null, errorCode: null, statusCode: null, operation, listId)),
            MsalUiRequiredException => Unauthorized(
                "The configured Microsoft account requires interactive reauthorization.",
                CreateDetails("microsoft-identity", "interaction_required", null, operation, listId)),
            MsalServiceException serviceException when IsTransientMsal(serviceException) =>
                TemporarilyUnavailable(details: CreateDetails("microsoft-identity", serviceException.ErrorCode, serviceException.StatusCode, operation, listId)),
            MsalServiceException serviceException when IsUnauthorizedMsal(serviceException) =>
                Unauthorized(
                    "The configured Microsoft account is not authorized to access Microsoft To Do.",
                    CreateDetails("microsoft-identity", serviceException.ErrorCode, serviceException.StatusCode, operation, listId)),
            MsalServiceException serviceException =>
                UpstreamFailure(details: CreateDetails("microsoft-identity", serviceException.ErrorCode, serviceException.StatusCode, operation, listId)),
            ApiException apiException when IsTransientGraph(apiException) =>
                TemporarilyUnavailable(details: CreateDetails("microsoft-graph", null, apiException.ResponseStatusCode, operation, listId)),
            ApiException apiException when IsUnauthorizedGraph(apiException) =>
                Unauthorized(
                    "The configured Microsoft account is not authorized to access Microsoft To Do.",
                    CreateDetails("microsoft-graph", null, apiException.ResponseStatusCode, operation, listId)),
            ApiException apiException =>
                UpstreamFailure(details: CreateDetails("microsoft-graph", null, apiException.ResponseStatusCode, operation, listId)),
            HttpRequestException => TemporarilyUnavailable(details: CreateDetails("microsoft-graph", null, null, operation, listId)),
            OperationCanceledException => TemporarilyUnavailable(details: CreateDetails("microsoft-graph", null, null, operation, listId)),
            TimeoutException => TemporarilyUnavailable(details: CreateDetails("microsoft-graph", null, null, operation, listId)),
            _ => UpstreamFailure(details: CreateDetails(provider: null, errorCode: null, statusCode: null, operation, listId))
        };
    }

    private static bool IsTransientMsal(MsalServiceException exception)
    {
        return exception.StatusCode is 408 or 429 or 500 or 502 or 503 or 504
            || string.Equals(exception.ErrorCode, "temporarily_unavailable", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnauthorizedMsal(MsalServiceException exception)
    {
        return exception.StatusCode is 401 or 403
            || string.Equals(exception.ErrorCode, "invalid_grant", StringComparison.OrdinalIgnoreCase)
            || string.Equals(exception.ErrorCode, "invalid_client", StringComparison.OrdinalIgnoreCase)
            || string.Equals(exception.ErrorCode, "unauthorized_client", StringComparison.OrdinalIgnoreCase)
            || string.Equals(exception.ErrorCode, "interaction_required", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTransientGraph(ApiException exception)
    {
        return exception.ResponseStatusCode is 408 or 429 or 502 or 503 or 504;
    }

    private static bool IsUnauthorizedGraph(ApiException exception)
    {
        return exception.ResponseStatusCode is 401 or 403;
    }

    private static IReadOnlyDictionary<string, object?> CreateDetails(
        string? provider,
        string? errorCode,
        int? statusCode,
        string? operation,
        string? listId)
    {
        var details = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(provider))
        {
            details["provider"] = provider;
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            details["errorCode"] = errorCode;
        }

        if (statusCode is not null and > 0)
        {
            details["statusCode"] = statusCode;
        }

        if (!string.IsNullOrWhiteSpace(operation))
        {
            details["operation"] = operation;
        }

        if (!string.IsNullOrWhiteSpace(listId))
        {
            details["listId"] = listId;
        }

        return details;
    }
}

public sealed record MappedFailure(HttpStatusCode StatusCode, ApiErrorDescriptor Error);