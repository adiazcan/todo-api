using System.Text.Json;
using FluentAssertions;
using TodoApi.Functions.Contracts;
using Xunit;

namespace TodoApi.ContractTests;

public sealed class SiriPayloadContractTests
{
    [Fact]
    public void PostTasks_should_publish_a_flat_siri_shortcuts_compatible_request_schema()
    {
        var contract = LoadOpenApiContract();

        contract.Should().Contain("CreateTaskRequest:");
        contract.Should().Contain("type: object");
        contract.Should().Contain("additionalProperties: false");
        contract.Should().Contain("required:");
        contract.Should().Contain("- text");
        contract.Should().Contain("- listId");
        contract.Should().Contain("application/json:");
        contract.Should().Contain("functionKey:");
        contract.Should().Contain("name: x-functions-key");
    }

    [Fact]
    public void CreateTaskRequest_should_serialize_to_the_published_siri_payload_shape()
    {
        var payload = new CreateTaskRequest
        {
            Text = "Pick up groceries",
            ListId = "list-456"
        };

        var json = JsonSerializer.SerializeToDocument(payload);
        var root = json.RootElement;

        root.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["text", "listId"]);

        root.GetProperty("text").GetString().Should().Be("Pick up groceries");
        root.GetProperty("listId").GetString().Should().Be("list-456");
    }

    [Fact]
    public void CreateTaskRequest_should_deserialize_the_minimal_siri_shortcuts_body()
    {
        var payload = JsonSerializer.Deserialize<CreateTaskRequest>("""
            {
              "text": "Pick up groceries",
              "listId": "list-456"
            }
            """);

        payload.Should().NotBeNull();
        payload!.Text.Should().Be("Pick up groceries");
        payload.ListId.Should().Be("list-456");
    }

    private static string LoadOpenApiContract()
    {
        var contractPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../specs/001-todo-task-api/contracts/openapi.yaml"));

        return File.ReadAllText(contractPath);
    }
}