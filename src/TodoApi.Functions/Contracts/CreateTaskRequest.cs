using System.Text.Json.Serialization;

namespace TodoApi.Functions.Contracts;

public sealed class CreateTaskRequest
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("listId")]
    public required string ListId { get; init; }
}