namespace TodoApi.Functions.Auth;

public interface IGraphAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}