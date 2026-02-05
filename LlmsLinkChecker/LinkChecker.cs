namespace LlmsLinkChecker;

/// <summary>
/// HTTP client for checking link validity.
/// </summary>
public class LinkChecker : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _verbose;
    private readonly int _maxConcurrency;
    private readonly SemaphoreSlim _semaphore;

    public LinkChecker(bool verbose = false, int maxConcurrency = 10, int timeoutSeconds = 30)
    {
        _verbose = verbose;
        _maxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency);
        
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LlmsLinkChecker/1.0");
    }

    /// <summary>
    /// Checks a single URL and returns the result.
    /// </summary>
    public async Task<LinkCheckResult> CheckLinkAsync(ExtractedLink link, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
            {
                return new LinkCheckResult(link, 0, false, "Invalid URL format");
            }

            if (_verbose)
            {
                Console.WriteLine($"  Checking: {link.Url}");
            }

            try
            {
                using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var statusCode = (int)response.StatusCode;
                var isSuccess = response.IsSuccessStatusCode;
                
                return new LinkCheckResult(link, statusCode, isSuccess, isSuccess ? null : response.ReasonPhrase);
            }
            catch (HttpRequestException ex)
            {
                return new LinkCheckResult(link, 0, false, $"HTTP Error: {ex.Message}");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new LinkCheckResult(link, 0, false, "Request timed out");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Fetches the content of a URL (for following llms.txt links).
    /// </summary>
    public async Task<(bool Success, string? Content, string? Error)> FetchContentAsync(string url, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return (false, null, "Invalid URL format");
            }

            try
            {
                var response = await _httpClient.GetAsync(uri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    return (true, content, null);
                }
                return (false, null, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (HttpRequestException ex)
            {
                return (false, null, $"HTTP Error: {ex.Message}");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return (false, null, "Request timed out");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _semaphore.Dispose();
    }
}

/// <summary>
/// Result of checking a single link.
/// </summary>
public record LinkCheckResult(
    ExtractedLink Link,
    int StatusCode,
    bool IsSuccess,
    string? ErrorMessage);
