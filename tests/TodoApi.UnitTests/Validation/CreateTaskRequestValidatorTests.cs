using FluentAssertions;
using TodoApi.Functions.Contracts;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.UnitTests.Validation;

public sealed class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new(new TodoErrorMapper());

    [Fact]
    public void Validate_should_reject_blank_task_text()
    {
        var result = _validator.Validate(new CreateTaskRequest
        {
            Text = "   ",
            ListId = "list-123"
        }, DateTimeOffset.Parse("2026-03-14T10:15:00Z"));

        result.IsValid.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        result.Failure.Error.Code.Should().Be("validation.task_text_required");
    }

    [Fact]
    public void Validate_should_reject_blank_list_id()
    {
        var result = _validator.Validate(new CreateTaskRequest
        {
            Text = "Buy milk",
            ListId = "   "
        }, DateTimeOffset.Parse("2026-03-14T10:15:00Z"));

        result.IsValid.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        result.Failure.Error.Code.Should().Be("validation.list_id_required");
    }

    [Fact]
    public void Validate_should_return_a_trimmed_command_for_a_valid_request()
    {
        var result = _validator.Validate(new CreateTaskRequest
        {
            Text = "  Buy milk  ",
            ListId = "  list-123  "
        }, DateTimeOffset.Parse("2026-03-14T10:15:00Z"));

        result.IsValid.Should().BeTrue();
        result.Command.Should().NotBeNull();
        result.Command!.Title.Should().Be("Buy milk");
        result.Command.ListId.Should().Be("list-123");
        result.Command.RequestedAtUtc.Should().Be(DateTimeOffset.Parse("2026-03-14T10:15:00Z"));
    }
}