using System.Text.Json;
using FluentAssertions;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.ContractTests;

public sealed class CreateTaskSuccessContractTests
{
    [Fact]
    public void PostTasks_should_publish_created_response_with_task_and_list_identifiers()
    {
        var contract = LoadOpenApiContract();

        contract.Should().Contain("openapi: 3.1.0");
        contract.Should().Contain("/api/tasks:");
        contract.Should().Contain("summary: Create a task in a caller-specified Microsoft To Do list");
        contract.Should().Contain("'201':");
        contract.Should().Contain("$ref: '#/components/schemas/CreateTaskSuccessResponse'");
        contract.Should().Contain("taskId:");
        contract.Should().Contain("title:");
        contract.Should().Contain("listId:");
        contract.Should().Contain("listName:");
        contract.Should().Contain("createdAtUtc:");
    }

    [Fact]
    public void Success_response_payload_should_serialize_the_published_fields()
    {
        var payload = new ApiResponseFactory().CreateTaskCreated(
            taskId: "task-123",
            title: "Pick up groceries",
            listId: "list-456",
            listName: "Errands",
            createdAtUtc: DateTimeOffset.Parse("2026-03-14T15:05:22Z"));

        var json = JsonSerializer.SerializeToDocument(payload);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetProperty("taskId").GetString().Should().Be("task-123");
        data.GetProperty("title").GetString().Should().Be("Pick up groceries");
        data.GetProperty("listId").GetString().Should().Be("list-456");
        data.GetProperty("listName").GetString().Should().Be("Errands");
        data.GetProperty("createdAtUtc").GetDateTimeOffset().Should().Be(DateTimeOffset.Parse("2026-03-14T15:05:22Z"));
    }

    private static string LoadOpenApiContract()
    {
        var contractPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../specs/001-todo-task-api/contracts/openapi.yaml"));

        return File.ReadAllText(contractPath);
    }
}