using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Neon.Core.Services.Http;

public class HttpService(ILogger<HttpService> logger, IHttpClientFactory httpClientFactory) : IHttpService
{
    private readonly ILogger<HttpService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private void ConfigureClient(HttpClient httpClient, string? method, string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers)
    {
        ArgumentException.ThrowIfNullOrEmpty(method, nameof(method));
        ArgumentException.ThrowIfNullOrEmpty(url, nameof(url));

        //if (!method.Equals("GET"))
        //{
        //    ArgumentNullException.ThrowIfNull(content, nameof(content));
        //    ArgumentException.ThrowIfNullOrEmpty(contentType, nameof(contentType));
        //}

        httpClient.DefaultRequestHeaders.Clear();

        if (headers is not null && headers.Count > 0)
            foreach (var key in headers.Keys)
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(key, headers[key]);

        httpClient.DefaultRequestHeaders.Authorization = authHeader;
    }

    public async Task<HttpResponseMessage?> GetAsync(string? url, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, "GET", url, null, string.Empty, authHeader, headers);
        return await httpClient.GetAsync(new Uri(url!), cancellationToken);
    }

    public async Task<HttpResponseMessage?> PostAsync(string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, "POST", url, content, contentType, authHeader, headers);
        return await httpClient.PostAsync(new Uri(url!), content, cancellationToken);
    }

    public async Task<HttpResponseMessage?> PutAsync(string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, "PUT", url, content, contentType, authHeader, headers);
        return await httpClient.PutAsync(new Uri(url!), content, cancellationToken);
    }

    public async Task<HttpResponseMessage?> PatchAsync(string? url, HttpContent? content, string? contentType, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, "PATCH", url, content, contentType, authHeader, headers);
        return await httpClient.PatchAsync(new Uri(url!), content, cancellationToken);
    }

    public async Task<HttpResponseMessage?> DeleteAsync(string? url, AuthenticationHeaderValue? authHeader, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, "DELETE", url, null, string.Empty, authHeader, headers);
        return await httpClient.DeleteAsync(new Uri(url!), cancellationToken);
    }
}
