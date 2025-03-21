using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace Neon.Core.Services.Http;

public class HttpService(ILogger<HttpService> logger, HttpClient httpClient) : IHttpService
{
    private readonly ILogger<HttpService> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;

    private void ConfigureClient(string? method, string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers)
    {
        ArgumentException.ThrowIfNullOrEmpty(method, nameof(method));
        ArgumentException.ThrowIfNullOrEmpty(url, nameof(url));

        if (!method.Equals("GET"))
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            ArgumentException.ThrowIfNullOrEmpty(contentType, nameof(contentType));
        }

        _httpClient.DefaultRequestHeaders.Clear();

        if (headers is not null && headers.Count > 0)
            foreach (var key in headers.Keys)
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(key, headers[key]);

        _httpClient.DefaultRequestHeaders.Authorization = authHeader;
    }

    public async Task<HttpResponseMessage?> GetAsync(string? url, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        ConfigureClient("GET", url, null, string.Empty, authHeader, headers);
        return await _httpClient.GetAsync(new Uri(url!), cancellationToken);
    }

    public async Task<HttpResponseMessage?> PostAsync(string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        ConfigureClient("POST", url, content, contentType, authHeader, headers);
        return await _httpClient.PostAsync(new Uri(url!), content, cancellationToken);
    }

    public async Task<HttpResponseMessage?> PutAsync(string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        ConfigureClient("PUT", url, content, contentType, authHeader, headers);
        return await _httpClient.PutAsync(new Uri(url!), content, cancellationToken);
    }

    public async Task<HttpResponseMessage?> PatchAsync(string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        ConfigureClient("PATCH", url, content, contentType, authHeader, headers);
        return await _httpClient.PatchAsync(new Uri(url!), content, cancellationToken);
    }

    public async Task<HttpResponseMessage?> DeleteAsync(string? url, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        ConfigureClient("DELETE", url, null, string.Empty, authHeader, headers);
        return await _httpClient.DeleteAsync(new Uri(url!), cancellationToken);
    }
}
