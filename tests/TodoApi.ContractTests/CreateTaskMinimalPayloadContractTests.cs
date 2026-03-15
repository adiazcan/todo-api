using System.Text.Json;
using FluentAssertions;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.ContractTests;

public sealed class CreateTaskMinimalPayloadContractTests
{
    [Fact]
    public void PostTasks_should_publish_the_minimal_request_body_and_reject_unknown_fields()
    {
        var contract = LoadOpenApiContract();

        contract.Should().Contain("CreateTaskRequest:");
        contract.Should().Contain("additionalProperties: false");
        contract.Should().Contain("required:");
        contract.Should().Contain("- text");
        contract.Should().Contain("- listId");
        contract.Should().Contain("minimal:");
        contract.Should().Contain("text: Pick up groceries");
        contract.Should().Contain("listId: AQMkAGI2...");
    }

    [Fact]
    public void Success_response_payload_should_not_serialize_fields_outside_the_published_v1_contract()
    {
        var payload = new ApiResponseFactory().CreateTaskCreated(
            taskId: "task-123",
            title: "Pick up groceries",
            listId: "list-456",
            listName: "Errands",
            createdAtUtc: DateTimeOffset.Parse("2026-03-14T15:05:22Z"));

        var json = JsonSerializer.SerializeToDocument(payload);
        var root = json.RootElement;
        var data = root.GetProperty("data");

        root.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["success", "data"]);

        data.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["taskId", "title", "listId", "listName", "createdAtUtc"]);
    }

    private static string LoadOpenApiContract()
    {
        var contractPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../specs/001-todo-task-api/contracts/openapi.yaml"));

        return File.ReadAllText(contractPath);
    }
}