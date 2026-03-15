using TodoApi.Functions.Contracts;

namespace TodoApi.Functions.Services;

public sealed class CreateTaskRequestValidator
{
    private readonly TodoErrorMapper _todoErrorMapper;

    public CreateTaskRequestValidator(TodoErrorMapper todoErrorMapper)
    {
        _todoErrorMapper = todoErrorMapper;
    }

    public ValidationResult Validate(CreateTaskRequest? request, DateTimeOffset requestedAtUtc)
    {
        if (request is null)
        {
            return ValidationResult.Failed(_todoErrorMapper.InvalidRequest());
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return ValidationResult.Failed(_todoErrorMapper.TaskTextRequired());
        }

        if (string.IsNullOrWhiteSpace(request.ListId))
        {
            return ValidationResult.Failed(_todoErrorMapper.ListIdRequired());
        }

        return ValidationResult.Succeeded(new NormalizedTaskCommand(
            Title: request.Text.Trim(),
            ListId: request.ListId.Trim(),
            RequestedAtUtc: requestedAtUtc));
    }
}