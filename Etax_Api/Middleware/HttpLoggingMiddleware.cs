using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Etax_Api.Middleware
{
    public class HttpLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpLoggingMiddleware> _logger;

        private readonly string[] _sensitiveKeys = new[] { "password", "token", "secret", "pdf_base64" };

        public HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task Invoke(HttpContext context)
        {
            // ✅ กรองเฉพาะ path ที่ต้องการ
            var targetPath = "/tripetch/create_etax";
            if (!context.Request.Path.Value.Contains(targetPath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            context.Request.EnableBuffering();
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            var maskedRequestBody = MaskSensitiveData(requestBody);

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();

            var logPayload = new
            {
                HttpLog = true,
                Method = context.Request.Method,
                Path = context.Request.Path,
                QueryString = context.Request.QueryString.Value,
                ClientIp = ip,
                UserAgent = userAgent,
                StatusCode = context.Response.StatusCode,
                RequestBody = maskedRequestBody,
                ResponseBody = responseBodyText
            };

         
                _logger.LogInformation("HTTP Log {@Payload}", logPayload);
            
        }

        private object? MaskSensitiveData(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                return MaskElement(root);
            }
            catch
            {
                return json;
            }
        }

        private object MaskElement(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return element.ToString();

            var result = new Dictionary<string, object>();

            foreach (var prop in element.EnumerateObject())
            {
                var key = prop.Name.ToLower();
                if (_sensitiveKeys.Contains(key))
                {
                    result[prop.Name] = "*****";
                }
                else if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    result[prop.Name] = MaskElement(prop.Value);
                }
                else if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    result[prop.Name] = prop.Value.EnumerateArray()
                        .Select(item => item.ValueKind == JsonValueKind.Object ? MaskElement(item) : item.ToString())
                        .ToList();
                }
                else
                {
                    result[prop.Name] = prop.Value.ToString();
                }
            }

            return result;
        }
    }
}
