using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TriviaGame.Mobile.Models;

namespace TriviaGame.Mobile.Services;

// ApiClient הוא השכבה הנמוכה שמדברת עם ה-API.
// כל בקשה עוברת דרכו, והוא מוסיף headers, timeout, retries וטיפול בשגיאות.
public sealed class ApiClient
{
    private readonly HttpClient httpClient;
    private readonly ApiEndpointResolver endpointResolver;

    // JSON options משותפים כדי ש-deserialization יעבוד בלי תלות בדיוק של אותיות גדולות/קטנות.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient httpClient, ApiEndpointResolver endpointResolver)
    {
        this.httpClient = httpClient;
        this.endpointResolver = endpointResolver;
    }

    // GET הוא מסלול הקריאה הנפוץ.
    // אין body, רק path, headers, ותשובת JSON שממופים חזרה לטיפוס T.
    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<object, T>(HttpMethod.Get, path, null, cancellationToken);

    // POST שולח payload ב-body ל-API.
    public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest payload, CancellationToken cancellationToken = default) =>
        SendAsync<TRequest, TResponse>(HttpMethod.Post, path, payload, cancellationToken);

    // PUT דומה ל-POST, רק למסלולי עדכון.
    public Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest payload, CancellationToken cancellationToken = default) =>
        SendAsync<TRequest, TResponse>(HttpMethod.Put, path, payload, cancellationToken);

    // DELETE בדרך כלל לא צריך body.
    public Task<ApiResult<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<object, TResponse>(HttpMethod.Delete, path, null, cancellationToken);

    private async Task<ApiResult<TResponse>> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string path,
        TRequest? payload,
        CancellationToken cancellationToken)
    {
        // ה-base URL נקבע לפי environment/מכשיר, ואז מצמידים אליו את ה-path של ה-endpoint.
        var requestUri = $"{endpointResolver.GetBaseUrl()}/{path.TrimStart('/')}";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            // בכל ניסיון בונים בקשה חדשה כדי שלא נשתמש באותו request object פגום.
            using var request = new HttpRequestMessage(method, requestUri);

            // זה ה-header שהשרת בודק לפני שהוא נותן גישה ל-/api.
            // כך כל קריאה מהאפליקציה מזדהה עם אותו app code פשוט.
            request.Headers.TryAddWithoutValidation("X-App-Code", endpointResolver.GetAppCode());

            // רק בקשות שאינן GET מקבלות body.
            if (payload is not null && method != HttpMethod.Get)
                request.Content = JsonContent.Create(payload);

            // timeout נפרד לכל ניסיון.
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(TimeSpan.FromSeconds(12));

            try
            {
                // שולחים את הבקשה בפועל ומקבלים תשובה מה-API.
                var response = await httpClient.SendAsync(request, attemptCts.Token);
                var statusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    // אם אין גוף תשובה, מחזירים Ok ריק.
                    if (typeof(TResponse) == typeof(object) || response.Content.Headers.ContentLength == 0)
                        return ApiResult<TResponse>.Ok(default, statusCode);

                    // אם יש JSON, ממירים אותו לטיפוס התוצאה שמצפה לו ה-UI.
                    var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(responseText))
                        return ApiResult<TResponse>.Ok(default, statusCode);

                    var value = JsonSerializer.Deserialize<TResponse>(responseText, JsonOptions);
                    return ApiResult<TResponse>.Ok(value, statusCode);
                }

                // כשיש שגיאה, מחלצים הודעה נוחה לקריאה כדי להציג אותה למשתמש.
                var message = await ExtractErrorMessageAsync(response, cancellationToken);

                // על קודיי HTTP זמניים אפשר לנסות שוב.
                if (ShouldRetry(response.StatusCode, attempt))
                {
                    await Task.Delay(GetBackoff(attempt), cancellationToken);
                    continue;
                }

                return ApiResult<TResponse>.Fail(message, statusCode);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // timeout לא אומר שהמשתמש ביטל, אלא שהשרת לקח יותר מדי זמן.
                if (attempt < 3)
                {
                    await Task.Delay(GetBackoff(attempt), cancellationToken);
                    continue;
                }

                return ApiResult<TResponse>.Fail($"Request timed out: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                // רשת לא זמינה, DNS, שרת לא עולה, או תקשורת כלשהי שנפלה.
                if (attempt < 3)
                {
                    await Task.Delay(GetBackoff(attempt), cancellationToken);
                    continue;
                }

                return ApiResult<TResponse>.Fail($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // כל כשל לא צפוי חוזר כהודעה כללית כדי לא להפיל את ה-UI.
                return ApiResult<TResponse>.Fail($"Unexpected error: {ex.Message}");
            }
        }

        return ApiResult<TResponse>.Fail("Request failed after retries.");
    }

    // מחליטים אם שווה לנסות שוב.
    // 408/429/5xx הם מצבים קלאסיים של כשל זמני.
    private static bool ShouldRetry(HttpStatusCode statusCode, int attempt)
    {
        if (attempt >= 3)
            return false;

        var code = (int)statusCode;
        return code == 408 || code == 429 || code >= 500;
    }

    // backoff קטן כדי לא להפציץ את השרת בבת אחת.
    private static TimeSpan GetBackoff(int attempt) =>
        TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));

    // מנסה לקרוא הודעת error מתוך JSON של השרת.
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
            // אם הגוף לא JSON, נשארים עם הודעת fallback.
        }

        return fallback;
    }
}
