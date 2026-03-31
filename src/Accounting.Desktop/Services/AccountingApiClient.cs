using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Accounting.Application.DTOs;

namespace Accounting.Desktop.Services;

public sealed class ApiResult<T>
{
    public ApiResult(bool ok, string? errorMessage, T? value)
    {
        Ok = ok;
        ErrorMessage = errorMessage;
        Value = value;
    }

    public bool Ok { get; }
    public string? ErrorMessage { get; }
    public T? Value { get; }
}

public sealed class AccountingApiClient : IDisposable
{
    private readonly HttpClient _http;
    /// <summary>Separate client for large backup/import downloads — long timeout set at creation only.
    /// <see cref="HttpClient.Timeout"/> must not be changed after the first request on an instance.</summary>
    private readonly HttpClient _httpBackup;
    private readonly JsonSerializerOptions _json;

    public AccountingApiClient(string baseUrl)
    {
        var baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
        _http = new HttpClient
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(45)
        };
        _httpBackup = new HttpClient
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromMinutes(30)
        };
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public string BaseUrl => _http.BaseAddress?.ToString().TrimEnd('/') ?? "";

    public void SetSessionToken(string? token)
    {
        const string header = "X-Session-Token";
        _http.DefaultRequestHeaders.Remove(header);
        _httpBackup.DefaultRequestHeaders.Remove(header);
        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation(header, token);
            _httpBackup.DefaultRequestHeaders.TryAddWithoutValidation(header, token);
        }
    }

    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.GetAsync("api/health", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<ApiResult<LoginResponseDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync<LoginResponseDto, LoginRequest>("api/auth/login", request, cancellationToken);

    public async Task<ApiResult<SessionInfoDto>> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.GetAsync("api/auth/session", cancellationToken).ConfigureAwait(false);
            var err = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<SessionInfoDto>(false, err, default);
            var value = await response.Content.ReadFromJsonAsync<SessionInfoDto>(_json, cancellationToken).ConfigureAwait(false);
            return new ApiResult<SessionInfoDto>(true, null, value);
        }
        catch (Exception ex)
        {
            return new ApiResult<SessionInfoDto>(false, ex.Message, default);
        }
    }

    public async Task<ApiResult<object?>> PostEmptyAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.PostAsync(relativeUrl, null, cancellationToken).ConfigureAwait(false);
            var err = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<object?>(false, err, default);
            return new ApiResult<object?>(true, null, null);
        }
        catch (Exception ex)
        {
            return new ApiResult<object?>(false, ex.Message, default);
        }
    }

    public async Task<ApiResult<object?>> LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.PostAsync("api/auth/logout", null, cancellationToken).ConfigureAwait(false);
            var err = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<object?>(false, err, default);
            return new ApiResult<object?>(true, null, null);
        }
        catch (Exception ex)
        {
            return new ApiResult<object?>(false, ex.Message, default);
        }
    }

    public async Task<ApiResult<object?>> PutJsonNoContentAsync<TRequest>(string relativeUrl, TRequest body, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, relativeUrl)
            {
                Content = JsonContent.Create(body, options: _json)
            };
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var err = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<object?>(false, err, default);
            return new ApiResult<object?>(true, null, null);
        }
        catch (Exception ex)
        {
            return new ApiResult<object?>(false, ex.Message, default);
        }
    }

    public async Task<IReadOnlyList<CompanyQueryDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.GetAsync("api/Companies", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return Array.Empty<CompanyQueryDto>();
            var list = await response.Content.ReadFromJsonAsync<List<CompanyQueryDto>>(_json, cancellationToken).ConfigureAwait(false);
            return list ?? new List<CompanyQueryDto>();
        }
        catch
        {
            return Array.Empty<CompanyQueryDto>();
        }
    }

    /// <summary>GET relative URL (e.g. api/companies/1/inquiry/journal-entries). Returns raw JSON or null on failure.</summary>
    public async Task<string?> GetJsonStringAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<T?> GetFromJsonAsync<T>(string relativeUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return default;
            return await response.Content.ReadFromJsonAsync<T>(_json, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>Configured backup root on the API host (requires backup permission).</summary>
    public Task<BackupFolderDto?> GetBackupFolderAsync(CancellationToken cancellationToken = default) =>
        GetFromJsonAsync<BackupFolderDto>("api/database-backup/folder", cancellationToken);

    public async Task<ApiResult<TResponse>> PostJsonAsync<TResponse, TRequest>(string relativeUrl, TRequest body, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(relativeUrl, body, _json, cancellationToken).ConfigureAwait(false);
            var err = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<TResponse>(false, err, default);
            var value = await response.Content.ReadFromJsonAsync<TResponse>(_json, cancellationToken).ConfigureAwait(false);
            return new ApiResult<TResponse>(true, null, value);
        }
        catch (Exception ex)
        {
            return new ApiResult<TResponse>(false, ex.Message, default);
        }
    }

    public async Task<ApiResult<TResponse>> PutJsonAsync<TResponse, TRequest>(string relativeUrl, TRequest body, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, relativeUrl)
            {
                Content = JsonContent.Create(body, options: _json)
            };
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var err = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<TResponse>(false, err, default);
            var value = await response.Content.ReadFromJsonAsync<TResponse>(_json, cancellationToken).ConfigureAwait(false);
            return new ApiResult<TResponse>(true, null, value);
        }
        catch (Exception ex)
        {
            return new ApiResult<TResponse>(false, ex.Message, default);
        }
    }

    public async Task<ApiResult<object?>> DeleteAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.DeleteAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
            var err = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<object?>(false, err, default);
            return new ApiResult<object?>(true, null, null);
        }
        catch (Exception ex)
        {
            return new ApiResult<object?>(false, ex.Message, default);
        }
    }

    /// <summary>Download binary response (large backups). Saves to <paramref name="destinationPath"/>.</summary>
    public async Task<ApiResult<string?>> DownloadBackupFileAsync(
        string relativeUrl,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpBackup.GetAsync(relativeUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            var err = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<string?>(false, err, default);
            await using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            return new ApiResult<string?>(true, null, destinationPath);
        }
        catch (Exception ex)
        {
            return new ApiResult<string?>(false, ex.Message, default);
        }
    }

    public async Task<ApiResult<ImportBackupResponseDto>> ImportBackupZipAsync(
        string zipPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var fs = File.OpenRead(zipPath);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fs), "file", Path.GetFileName(zipPath));
            using var response = await _httpBackup.PostAsync("api/database-backup/import-json", content, cancellationToken)
                .ConfigureAwait(false);
            var err = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await TryReadImportBackupResponseAsync(response, _json, cancellationToken)
                    .ConfigureAwait(false);
                return new ApiResult<ImportBackupResponseDto>(false,
                    body?.ErrorMessage ?? err,
                    body ?? new ImportBackupResponseDto { Ok = false, ErrorMessage = err });
            }

            var ok = await response.Content.ReadFromJsonAsync<ImportBackupResponseDto>(_json, cancellationToken)
                .ConfigureAwait(false);
            return new ApiResult<ImportBackupResponseDto>(true, null, ok ?? new ImportBackupResponseDto { Ok = true });
        }
        catch (Exception ex)
        {
            return new ApiResult<ImportBackupResponseDto>(false, ex.Message,
                new ImportBackupResponseDto { Ok = false, ErrorMessage = ex.Message });
        }
    }

    private static async Task<ImportBackupResponseDto?> TryReadImportBackupResponseAsync(
        HttpResponseMessage response,
        JsonSerializerOptions json,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ImportBackupResponseDto>(json, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return null;
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(text) ? $"{(int)response.StatusCode} {response.ReasonPhrase}" : text;
        }
        catch
        {
            return $"{(int)response.StatusCode} {response.ReasonPhrase}";
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        _httpBackup.Dispose();
    }
}
