using System.Buffers;
using System.Text;

namespace HttpBodyMiddleware.Middlewares;

/// <summary>
///     NOT WORKING YET.
/// </summary>
/// <param name="next"></param>
/// <param name="logger"></param>
public class BalancedHttpContextLoggingMiddleware(
    RequestDelegate next,
    ILogger<BalancedHttpContextLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();

        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            // Read request body safely
            var requestBody = await ReadRequestBody(context.Request).ConfigureAwait(false);

            // Store original response body stream
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            if (context.Response.StatusCode >= 400)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await ReadResponseBody(responseBodyStream).ConfigureAwait(false);
                logger.BeginScope(new Dictionary<string, object>
                {
                    { "http.request.body", requestBody },
                    { "http.response.body", responseBody }
                });
                logger.LogWarning(requestBody);
                logger.LogWarning(responseBody);
                responseBodyStream.Seek(0, SeekOrigin.Begin);
            }

            await responseBodyStream.CopyToAsync(originalBodyStream).ConfigureAwait(false);
            context.Response.Body = originalBodyStream;
        }
    }

    // Not thread safe
    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync().ConfigureAwait(false);
        request.Body.Position = 0;
        return body;
    }

    private async Task<string> ReadResponseBody(Stream responseBodyStream)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        var sb = new StringBuilder();

        try
        {
            int bytesRead;
            while ((bytesRead = await responseBodyStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, true);
        }

        return sb.ToString();
    }
}