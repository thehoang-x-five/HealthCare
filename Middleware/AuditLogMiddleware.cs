using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using HealthCare.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace HealthCare.Middleware
{
    /// <summary>
    /// Middleware to capture and log all mutation requests (POST/PUT/DELETE/PATCH) to MongoDB audit_logs.
    /// Placed after UseAuthorization to access user claims.
    /// </summary>
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLogMiddleware> _logger;

        public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogRepository auditLogRepo)
        {
            var method = context.Request.Method.ToUpperInvariant();

            // Only log mutation operations
            if (method != "POST" && method != "PUT" && method != "DELETE" && method != "PATCH")
            {
                await _next(context);
                return;
            }

            // Skip logging for certain paths (health checks, swagger, SignalR)
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/swagger") ||
                path.StartsWith("/health") ||
                path.StartsWith("/hubs"))
            {
                await _next(context);
                return;
            }

            // Capture request details
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? context.User?.FindFirst("sub")?.Value
                         ?? "anonymous";

            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var resource = ExtractResourceFromPath(path);
            var resourceId = ExtractResourceIdFromPath(path);

            // Capture request body for POST/PUT/PATCH
            string? requestBody = null;
            if (method == "POST" || method == "PUT" || method == "PATCH")
            {
                requestBody = await ReadRequestBodyAsync(context.Request);
            }

            // Execute the request
            await _next(context);

            // Log after successful request (status code 2xx)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                var action = MapMethodToAction(method);

                BsonDocument? changes = null;
                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    try
                    {
                        changes = BsonDocument.Parse(requestBody);
                    }
                    catch
                    {
                        // If parsing fails, store as raw string
                        changes = new BsonDocument { { "raw_body", requestBody } };
                    }
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await auditLogRepo.LogAsync(
                            userId: userId,
                            action: action,
                            resource: resource,
                            resourceId: resourceId,
                            changes: changes,
                            ipAddress: ipAddress,
                            userAgent: userAgent
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Audit logging skipped for {Method} {Path}", method, path);
                    }
                });
            }
        }

        private static string ExtractResourceFromPath(string path)
        {
            // Extract resource name from path like /api/patients/123 -> patients
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                return segments[1];
            }

            return path;
        }

        private static string ExtractResourceIdFromPath(string path)
        {
            // Extract ID from path like /api/patients/123 -> 123
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 3 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                return segments[2];
            }

            return "unknown";
        }

        private static string MapMethodToAction(string method)
        {
            return method switch
            {
                "POST" => "create",
                "PUT" => "update",
                "PATCH" => "update",
                "DELETE" => "delete",
                _ => method.ToLowerInvariant()
            };
        }

        private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
        {
            try
            {
                request.EnableBuffering();

                using var reader = new StreamReader(
                    request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);

                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                return body;
            }
            catch
            {
                return null;
            }
        }
    }
}
