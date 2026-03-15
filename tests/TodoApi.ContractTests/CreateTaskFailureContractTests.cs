using System.Text.Json;
using FluentAssertions;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.ContractTests;

public sealed class CreateTaskFailureContractTests
{
    [Fact]
    public void PostTasks_should_publish_validation_and_dependency_failure_responses()
    {
        var contract = LoadOpenApiContract();

        contract.Should().Contain("'400':");
        contract.Should().Contain("validation.task_text_required");
        contract.Should().Contain("validation.list_id_required");
        contract.Should().Contain("'401':");
        contract.Should().Contain("auth.unauthorized");
        contract.Should().Contain("'502':");
        contract.Should().Contain("todo.upstream_failure");
        contract.Should().Contain("'503':");
        contract.Should().Contain("todo.temporarily_unavailable");
        contract.Should().Contain("$ref: '#/components/schemas/ErrorResponse'");
    }

    [Fact]
    public void Error_response_payload_should_serialize_the_published_failure_fields()
    {
        var failure = new TodoErrorMapper().TemporarilyUnavailable();
        var payload = new ApiResponseFactory().CreateError(failure.Error);

        var json = JsonSerializer.SerializeToDocument(payload);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = root.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("todo.temporarily_unavailable");
        error.GetProperty("message").GetString().Should().Be("Microsoft To Do is temporarily unavailable. Retry later.");
        error.GetProperty("retryable").GetBoolean().Should().BeTrue();
    }

    private static string LoadOpenApiContract()
    {
        var contractPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../specs/001-todo-task-api/contracts/openapi.yaml"));

        return File.ReadAllText(contractPath);
    }
}