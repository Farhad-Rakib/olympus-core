using Microsoft.AspNetCore.Mvc.Filters;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;

namespace ProjectNamePlaceholder.Api.Filters;

public sealed class AuditActionFilter : IAsyncResourceFilter
{
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Cookie", "Set-Cookie", "X-Api-Key"
    };

    private readonly IAuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditActionFilter(IAuditLogService auditLogService, IHttpContextAccessor httpContextAccessor)
    {
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var http = _httpContextAccessor.HttpContext;
        var user = http?.User;
        long? userId = null;
        string? userName = null;
        if (user != null)
        {
            var idClaim = user.FindFirst("sub") ?? user.FindFirst("id") ?? user.FindFirst("userId");
            if (idClaim != null && long.TryParse(idClaim.Value, out var id)) userId = id;
            userName = user.Identity?.Name ?? user.FindFirst("name")?.Value ?? user.FindFirst("email")?.Value;
        }

        // Prepare to capture request body
        string? requestBody = null;
        try
        {
            var req = http?.Request;
            if (req != null && req.ContentLength.GetValueOrDefault() > 0 && req.ContentType != null)
            {
                var lowerPath = req.Path.Value?.ToLowerInvariant() ?? string.Empty;
                // Skip capturing for sensitive endpoints
                if (!lowerPath.Contains("password") && !lowerPath.Contains("reset") && !lowerPath.Contains("token") && !lowerPath.Contains("auth"))
                {
                    if (req.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) || req.ContentType.StartsWith("text/"))
                    {
                        req.EnableBuffering();
                        using var reader = new StreamReader(req.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                        var raw = await reader.ReadToEndAsync();
                        req.Body.Position = 0;
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            requestBody = raw.Length > 4096 ? raw.Substring(0, 4096) : raw;
                        }
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        // Capture response by swapping the body. A resource filter wraps result execution,
        // so the buffer is still alive when the result is written.
        var originalBody = http?.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        if (http?.Response != null)
        {
            http.Response.Body = responseBodyStream;
        }

        ResourceExecutedContext? executedContext = null;
        try
        {
            executedContext = await next();
        }
        finally
        {
            try
            {
                if (http?.Response != null)
                {
                    http.Response.Body.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(http.Response.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    var resp = await reader.ReadToEndAsync();
                    http.Response.Body.Seek(0, SeekOrigin.Begin);

                    string? responseBody = null;
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(resp)) responseBody = resp.Length > 4096 ? resp.Substring(0, 4096) : resp;
                    }
                    catch { }

                    // copy the contents back to the original stream and restore it,
                    // so later writers (e.g. the exception handler) use the real body
                    if (originalBody != null)
                    {
                        http.Response.Body.Seek(0, SeekOrigin.Begin);
                        await http.Response.Body.CopyToAsync(originalBody);
                        http.Response.Body = originalBody;
                    }

                    var headers = http.Request?.Headers?
                        .Where(h => !SensitiveHeaders.Contains(h.Key))
                        .ToDictionary(k => k.Key, v => string.Join(',', v.Value.ToArray())) ?? new Dictionary<string, string>();

                    var entry = new AuditLog
                    {
                        UserId = userId,
                        UserName = userName,
                        Action = executedContext?.ActionDescriptor?.DisplayName ?? string.Empty,
                        Path = http.Request?.GetDisplayUrl() ?? string.Empty,
                        Method = http.Request?.Method ?? string.Empty,
                        Data = null,
                        RequestBody = requestBody,
                        ResponseBody = responseBody,
                        StatusCode = http.Response?.StatusCode,
                        UserAgent = http.Request?.Headers["User-Agent"].FirstOrDefault(),
                        Headers = JsonSerializer.Serialize(headers),
                        Success = executedContext?.Exception is null && http.Response?.StatusCode < 400,
                        Timestamp = DateTime.UtcNow,
                        Ip = http.Connection?.RemoteIpAddress?.ToString()
                    };

                    // Fire-and-forget persistence but avoid throwing
                    try
                    {
                        await _auditLogService.LogAsync(entry, CancellationToken.None);
                    }
                    catch
                    {
                        // swallow
                    }
                }
            }
            catch
            {
                // swallow
            }
            finally
            {
                if (http != null && originalBody != null)
                {
                    http.Response.Body = originalBody;
                }
            }
        }
    }
}
