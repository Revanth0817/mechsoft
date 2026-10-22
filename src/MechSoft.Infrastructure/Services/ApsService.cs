using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MechSoft.Infrastructure.Services;

public class ApsOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Optional. OSS bucket names are global, so the default is derived from the client id.</summary>
    public string BucketKey { get; set; } = string.Empty;
}

/// <summary>
/// Autodesk Platform Services: uploads a stored CAD file to OSS, asks Model Derivative to
/// translate it to SVF2, and hands the browser a read-only token for the APS Viewer.
/// </summary>
public class ApsService : IApsService
{
    private const string Base = "https://developer.api.autodesk.com";
    private const string InternalScope = "data:read data:write data:create bucket:create bucket:read";
    private const string ViewerScope = "viewables:read";

    private static readonly Dictionary<string, (string Token, DateTime Expires)> TokenCache = new();
    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static volatile bool _bucketReady;

    private readonly HttpClient _http;
    private readonly ApsOptions _options;
    private readonly ApplicationDbContext _context;

    public ApsService(HttpClient http, IOptions<ApsOptions> options, ApplicationDbContext context)
    {
        _http = http;
        _options = options.Value;
        _context = context;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ClientId) && !string.IsNullOrWhiteSpace(_options.ClientSecret);

    private string BucketKey => string.IsNullOrWhiteSpace(_options.BucketKey)
        ? ("mechsoft-" + _options.ClientId.ToLowerInvariant())[..Math.Min(128, 9 + _options.ClientId.Length)]
        : _options.BucketKey.ToLowerInvariant();

    public async Task<ApsTokenDto> GetViewerTokenAsync(CancellationToken ct = default)
    {
        var (token, expires) = await GetTokenAsync(ViewerScope, ct);
        return new ApsTokenDto(token, Math.Max(60, (int)(expires - DateTime.UtcNow).TotalSeconds));
    }

    public async Task<string> TranslateAsync(int cadFileId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var file = await _context.CadFiles.FirstOrDefaultAsync(f => f.Id == cadFileId, ct)
            ?? throw new InvalidOperationException("File not found.");

        var (token, _) = await GetTokenAsync(InternalScope, ct);
        await EnsureBucketAsync(token, ct);

        // The extension in the object name is how Model Derivative recognises the CAD type.
        var objectName = $"cad-{file.Id}.{file.Extension}";
        var objectId = await UploadAsync(token, objectName, file.Content, ct);
        var urn = Convert.ToBase64String(Encoding.UTF8.GetBytes(objectId)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var job = new
        {
            input = new { urn },
            output = new { formats = new[] { new { type = "svf2", views = new[] { "2d", "3d" } } } },
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{Base}/modelderivative/v2/designdata/job")
        {
            Content = JsonContent(job),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("x-ads-force", "true");
        await SendAsync(req, ct);

        file.ApsUrn = urn;
        file.ApsStatus = "pending";
        await _context.SaveChangesAsync(ct);
        return file.ApsStatus;
    }

    public async Task<string?> RefreshStatusAsync(int cadFileId, CancellationToken ct = default)
    {
        var file = await _context.CadFiles.FirstOrDefaultAsync(f => f.Id == cadFileId, ct);
        if (file is null || string.IsNullOrEmpty(file.ApsUrn)) return null;
        if (file.ApsStatus is "success" or "failed") return file.ApsStatus;
        EnsureConfigured();

        var (token, _) = await GetTokenAsync(InternalScope, ct);
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{Base}/modelderivative/v2/designdata/{file.ApsUrn}/manifest");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await _http.SendAsync(req, ct);

        string status;
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            status = "pending"; // manifest not created yet
        }
        else
        {
            var body = await ReadAsync(res, ct);
            var apsStatus = body.RootElement.TryGetProperty("status", out var s) ? s.GetString() : "inprogress";
            status = apsStatus switch { "success" => "success", "failed" or "timeout" => "failed", _ => "pending" };
        }

        if (status != file.ApsStatus)
        {
            file.ApsStatus = status;
            await _context.SaveChangesAsync(ct);
        }
        return status;
    }

    /* ---------------- helpers ---------------- */

    private void EnsureConfigured()
    {
        if (!IsConfigured) throw new InvalidOperationException("Autodesk Platform Services is not configured (missing Aps:ClientId / Aps:ClientSecret).");
    }

    private async Task<(string Token, DateTime Expires)> GetTokenAsync(string scope, CancellationToken ct)
    {
        EnsureConfigured();
        await TokenLock.WaitAsync(ct);
        try
        {
            if (TokenCache.TryGetValue(scope, out var cached) && cached.Expires > DateTime.UtcNow.AddMinutes(1)) return cached;

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{Base}/authentication/v2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["scope"] = scope,
                }),
            };
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            using var res = await _http.SendAsync(req, ct);
            var body = await ReadAsync(res, ct);
            var token = body.RootElement.GetProperty("access_token").GetString()!;
            var seconds = body.RootElement.GetProperty("expires_in").GetInt32();
            var entry = (token, DateTime.UtcNow.AddSeconds(seconds));
            TokenCache[scope] = entry;
            return entry;
        }
        finally
        {
            TokenLock.Release();
        }
    }

    private async Task EnsureBucketAsync(string token, CancellationToken ct)
    {
        if (_bucketReady) return;
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{Base}/oss/v2/buckets")
        {
            Content = JsonContent(new { bucketKey = BucketKey, policyKey = "persistent" }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await _http.SendAsync(req, ct);
        // 409 = the bucket already exists, which is fine.
        if (!res.IsSuccessStatusCode && res.StatusCode != System.Net.HttpStatusCode.Conflict)
            throw new InvalidOperationException($"Autodesk bucket setup failed ({(int)res.StatusCode}): {await res.Content.ReadAsStringAsync(ct)}");
        _bucketReady = true;
    }

    // Signed-URL upload: get an S3 URL, PUT the bytes (no auth header), then complete the upload.
    private async Task<string> UploadAsync(string token, string objectName, byte[] content, CancellationToken ct)
    {
        var endpoint = $"{Base}/oss/v2/buckets/{BucketKey}/objects/{Uri.EscapeDataString(objectName)}/signeds3upload";

        using var start = new HttpRequestMessage(HttpMethod.Get, endpoint + "?parts=1&firstPart=1");
        start.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var startDoc = await SendAsync(start, ct);
        var uploadKey = startDoc.RootElement.GetProperty("uploadKey").GetString()!;
        var url = startDoc.RootElement.GetProperty("urls")[0].GetString()!;

        using var put = new HttpRequestMessage(HttpMethod.Put, url) { Content = new ByteArrayContent(content) };
        using var putRes = await _http.SendAsync(put, ct);
        if (!putRes.IsSuccessStatusCode)
            throw new InvalidOperationException($"Upload to Autodesk failed ({(int)putRes.StatusCode}).");

        using var finish = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent(new { uploadKey }) };
        finish.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var doc = await SendAsync(finish, ct);
        return doc.RootElement.GetProperty("objectId").GetString()!;
    }

    private async Task<JsonDocument> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        using var res = await _http.SendAsync(req, ct);
        return await ReadAsync(res, ct);
    }

    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage res, CancellationToken ct)
    {
        var text = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Autodesk request failed ({(int)res.StatusCode}): {text}");
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
    }

    private static StringContent JsonContent(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
}
