using System.Text.Json.Serialization;

namespace TodoApi.Functions.Contracts;

public sealed class CreateTaskSuccessResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; } = true;

    [JsonPropertyName("data")]
    public required CreateTaskSuccessData Data { get; init; }
}

public sealed class CreateTaskSuccessData
{
    [JsonPropertyName("taskId")]
    public required string TaskId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("listId")]
    public required string ListId { get; init; }

    [JsonPropertyName("listName")]
    public required string ListName { get; init; }

    [JsonPropertyName("createdAtUtc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CreatedAtUtc { get; init; }
}