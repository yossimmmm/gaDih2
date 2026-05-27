using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TriviaGame.Mobile.Models;

namespace TriviaGame.Mobile.Services;

public sealed class ApiClient
{
    private readonly HttpClient httpClient;
    private readonly ApiEndpointResolver endpointResolver;
    private readonly AuthSessionStore authSessionStore;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(
        HttpClient httpClient,
        ApiEndpointResolver endpointResolver,
        AuthSessionStore authSessionStore)
    {
        this.httpClient = httpClient;
        this.endpointResolver = endpointResolver;
        this.authSessionStore = authSessionStore;
    }

    // קריאת GET עם retry בסיסי.
    public Task<ApiResult<T>> GetAsync<T>(string path, bool requiresAuth = false, CancellationToken cancellationToken = default) =>
        SendAsync<object, T>(HttpMethod.Get, path, null, requiresAuth, cancellationToken);

    // קריאת POST.
    public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest payload, bool requiresAuth = false, CancellationToken cancellationToken = default) =>
        SendAsync<TRequest, TResponse>(HttpMethod.Post, path, payload, requiresAuth, cancellationToken);

    // קריאת PUT.
    public Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest payload, bool requiresAuth = false, CancellationToken cancellationToken = default) =>
        SendAsync<TRequest, TResponse>(HttpMethod.Put, path, payload, requiresAuth, cancellationToken);

    // קריאת DELETE.
    public Task<ApiResult<TResponse>> DeleteAsync<TResponse>(string path, bool requiresAuth = false, CancellationToken cancellationToken = default) =>
        SendAsync<object, TResponse>(HttpMethod.Delete, path, null, requiresAuth, cancellationToken);

    // מנגנון שליחה מרכזי עם timeout + retries לשגיאות זמניות.
    private async Task<ApiResult<TResponse>> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string path,
        TRequest? payload,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        var baseUrl = endpointResolver.GetBaseUrl();
        var requestUri = $"{baseUrl}/{path.TrimStart('/')}";
        Exception? lastException = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var request = new HttpRequestMessage(method, requestUri);
            if (payload is not null && method != HttpMethod.Get)
                request.Content = JsonContent.Create(payload);

            if (requiresAuth)
            {
                var token = authSessionStore.GetToken();
                if (string.IsNullOrWhiteSpace(token))
                    return ApiResult<TResponse>.Fail("Not authenticated.");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(TimeSpan.FromSeconds(12));

            try
            {
                var response = await httpClient.SendAsync(request, attemptCts.Token);
                var statusCode = (int)response.StatusCode;

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // אם השרת מחזיר 401 - מוחקים session מקומי כדי להכריח login מחדש.
                    authSessionStore.Clear();
                    return ApiResult<TResponse>.Fail("Session expired. Please login again.", statusCode);
                }

                if (response.IsSuccessStatusCode)
                {
                    if (typeof(TResponse) == typeof(object) || response.Content.Headers.ContentLength == 0)
                        return ApiResult<TResponse>.Ok(default, statusCode);

                    var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(responseText))
                        return ApiResult<TResponse>.Ok(default, statusCode);

                    var value = JsonSerializer.Deserialize<TResponse>(responseText, JsonOptions);
                    return ApiResult<TResponse>.Ok(value, statusCode);
                }

                var message = await ExtractErrorMessageAsync(response, cancellationToken);
                if (ShouldRetry(response.StatusCode, attempt))
                {
                    await Task.Delay(GetBackoff(attempt), cancellationToken);
                    continue;
                }

                return ApiResult<TResponse>.Fail(message, statusCode);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                if (attempt < 3)
                {
                    await Task.Delay(GetBackoff(attempt), cancellationToken);
                    continue;
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                if (attempt < 3)
                {
                    await Task.Delay(GetBackoff(attempt), cancellationToken);
                    continue;
                }
            }
            catch (Exception ex)
            {
                return ApiResult<TResponse>.Fail($"Unexpected error: {ex.Message}");
            }
        }

        return ApiResult<TResponse>.Fail(lastException?.Message ?? "Request failed after retries.");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode, int attempt)
    {
        if (attempt >= 3)
            return false;
        var code = (int)statusCode;
        return code == 408 || code == 429 || code >= 500;
    }

    private static TimeSpan GetBackoff(int attempt) =>
        TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var fallback = $"Request failed ({(int)response.StatusCode}).";
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("message", out var messageNode))
                return messageNode.GetString() ?? fallback;
            if (doc.RootElement.TryGetProperty("title", out var titleNode))
                return titleNode.GetString() ?? fallback;
        }
        catch
        {
            // אם body אינו JSON פשוט מחזירים fallback.
        }

        return fallback;
    }
}
