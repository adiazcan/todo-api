using System.Net;

namespace TodoApi.Functions.Services;

public sealed class TodoTaskOperationException : Exception
{
    public TodoTaskOperationException(HttpStatusCode statusCode, ApiErrorDescriptor error, Exception? innerException = null)
        : base(error.Message, innerException)
    {
        StatusCode = statusCode;
        Error = error;
    }

    public HttpStatusCode StatusCode { get; }

    public ApiErrorDescriptor Error { get; }
}