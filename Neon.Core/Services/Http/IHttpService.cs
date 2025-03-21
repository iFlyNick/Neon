using System.Net.Http.Headers;

namespace Neon.Core.Services.Http;

public interface IHttpService
{
    Task<HttpResponseMessage?> GetAsync(string? url, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage?> PostAsync(string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage?> PutAsync(string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage?> PatchAsync(string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage?> DeleteAsync(string? url, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default);
}
