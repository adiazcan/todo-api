using System.Text.Json.Serialization;

namespace TodoApi.Functions.Contracts;

public sealed class ErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; } = false;

    [JsonPropertyName("error")]
    public required ErrorDetails Error { get; init; }
}

public sealed class ErrorDetails
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("retryable")]
    public bool Retryable { get; init; }

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object?>? Details { get; init; }
}