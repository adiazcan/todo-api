namespace TodoApi.Functions.Services;

public sealed record NormalizedTaskCommand(
    string Title,
    string ListId,
    DateTimeOffset RequestedAtUtc);