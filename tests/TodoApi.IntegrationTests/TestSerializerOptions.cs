using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TodoApi.IntegrationTests;

internal static class TestSerializerOptions
{
    internal static IOptions<JsonSerializerOptions> Create() =>
        Options.Create(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
}
