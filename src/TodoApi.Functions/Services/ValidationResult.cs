namespace TodoApi.Functions.Services;

public sealed class ValidationResult
{
    private ValidationResult(NormalizedTaskCommand? command, MappedFailure? failure)
    {
        Command = command;
        Failure = failure;
    }

    public bool IsValid => Failure is null;

    public NormalizedTaskCommand? Command { get; }

    public MappedFailure? Failure { get; }

    public static ValidationResult Succeeded(NormalizedTaskCommand command)
    {
        return new ValidationResult(command, failure: null);
    }

    public static ValidationResult Failed(MappedFailure failure)
    {
        return new ValidationResult(command: null, failure);
    }
}