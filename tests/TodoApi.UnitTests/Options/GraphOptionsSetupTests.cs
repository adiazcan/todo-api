using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TodoApi.Functions.Options;
using Xunit;

namespace TodoApi.UnitTests.Options;

public sealed class GraphOptionsSetupTests
{
    [Fact]
    public void Validate_should_succeed_when_a_serialized_user_token_cache_is_configured()
    {
        var setup = CreateSetup(new Dictionary<string, string?>
        {
            ["TodoApi:Graph:TenantId"] = "tenant-id",
            ["TodoApi:Graph:ClientId"] = "client-id",
            ["TodoApi:Graph:UserTokenCache"] = Convert.ToBase64String([1, 2, 3]),
            ["TodoApi:Graph:Scopes"] = "https://graph.microsoft.com/Tasks.ReadWrite offline_access"
        });

        var options = new GraphOptions();
        setup.Configure(options);

        var result = setup.Validate(name: null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_succeed_when_a_serialized_user_token_cache_contains_surrounding_whitespace()
    {
        var setup = CreateSetup(new Dictionary<string, string?>
        {
            ["TodoApi:Graph:TenantId"] = " tenant-id ",
            ["TodoApi:Graph:ClientId"] = " client-id ",
            ["TodoApi:Graph:UserTokenCache"] = $"  {Convert.ToBase64String([1, 2, 3])}  ",
            ["TodoApi:Graph:Scopes"] = "https://graph.microsoft.com/Tasks.ReadWrite offline_access"
        });

        var options = new GraphOptions();
        setup.Configure(options);

        var result = setup.Validate(name: null, options);

        result.Succeeded.Should().BeTrue();
        options.TenantId.Should().Be("tenant-id");
        options.ClientId.Should().Be("client-id");
    }

    [Fact]
    public void Validate_should_fail_when_neither_token_cache_nor_refresh_token_is_configured()
    {
        var setup = CreateSetup(new Dictionary<string, string?>
        {
            ["TodoApi:Graph:TenantId"] = "tenant-id",
            ["TodoApi:Graph:ClientId"] = "client-id",
            ["TodoApi:Graph:Scopes"] = "https://graph.microsoft.com/Tasks.ReadWrite offline_access"
        });

        var options = new GraphOptions();
        setup.Configure(options);

        var result = setup.Validate(name: null, options);

        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain("TodoApi:Graph:UserTokenCache or TodoApi:Graph:RefreshToken is required.");
    }

    [Fact]
    public void Validate_should_fail_when_the_serialized_token_cache_is_not_base64()
    {
        var setup = CreateSetup(new Dictionary<string, string?>
        {
            ["TodoApi:Graph:TenantId"] = "tenant-id",
            ["TodoApi:Graph:ClientId"] = "client-id",
            ["TodoApi:Graph:UserTokenCache"] = "not-base64",
            ["TodoApi:Graph:Scopes"] = "https://graph.microsoft.com/Tasks.ReadWrite offline_access"
        });

        var options = new GraphOptions();
        setup.Configure(options);

        var result = setup.Validate(name: null, options);

        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain("TodoApi:Graph:UserTokenCache must be a valid Base64-encoded MSAL cache payload. Re-run TodoApi.AuthBootstrap and copy the emitted value exactly as a single line.");
    }

    [Fact]
    public void Validate_should_require_client_secret_when_using_refresh_token_fallback()
    {
        var setup = CreateSetup(new Dictionary<string, string?>
        {
            ["TodoApi:Graph:TenantId"] = "tenant-id",
            ["TodoApi:Graph:ClientId"] = "client-id",
            ["TodoApi:Graph:RefreshToken"] = "refresh-token",
            ["TodoApi:Graph:Scopes"] = "https://graph.microsoft.com/Tasks.ReadWrite offline_access"
        });

        var options = new GraphOptions();
        setup.Configure(options);

        var result = setup.Validate(name: null, options);

        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain("TodoApi:Graph:ClientSecret is required when TodoApi:Graph:RefreshToken is configured.");
    }

    private static GraphOptionsSetup CreateSetup(IDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new GraphOptionsSetup(configuration);
    }
}