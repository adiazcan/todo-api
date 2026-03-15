using System.Net;
using FluentAssertions;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions;
using TodoApi.Functions.Services;
using Xunit;

namespace TodoApi.UnitTests.Services;

public sealed class TodoErrorMapperTests
{
    private readonly TodoErrorMapper _mapper = new();

    [Fact]
    public void Map_should_classify_reauthorization_requirements_as_unauthorized()
    {
        var failure = _mapper.Map(new MsalUiRequiredException("invalid_grant", "Interactive sign-in is required."));

        failure.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        failure.Error.Code.Should().Be("auth.unauthorized");
        failure.Error.Retryable.Should().BeFalse();
    }

    [Fact]
    public void Map_should_classify_transient_graph_errors_as_service_unavailable()
    {
        var failure = _mapper.Map(new StubApiException((int)HttpStatusCode.ServiceUnavailable));

        failure.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        failure.Error.Code.Should().Be("todo.temporarily_unavailable");
        failure.Error.Retryable.Should().BeTrue();
    }

    [Fact]
    public void Map_should_classify_non_transient_graph_errors_as_bad_gateway()
    {
        var failure = _mapper.Map(new StubApiException((int)HttpStatusCode.NotFound));

        failure.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        failure.Error.Code.Should().Be("todo.upstream_failure");
        failure.Error.Retryable.Should().BeFalse();
    }

    private sealed class StubApiException : ApiException
    {
        public StubApiException(int statusCode)
            : base("Microsoft Graph failed.")
        {
            ResponseStatusCode = statusCode;
        }
    }
}