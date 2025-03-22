using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace HttpBodyMiddleware.Middlewares;

/// <summary>
/// NOT WORKING YET.
/// </summary>
/// <param name="next"></param>
/// <param name="logger"></param>
public class SpeedHttpContextLoggingMiddleware(RequestDelegate next, ILogger<SpeedHttpContextLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();

        try
        {
            await next(context);
        }
        finally
        {
            // Read request body safely
            var requestBody = await ReadRequestBodyAsync(context.Request);

            // Store original response body stream
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            if (context.Response.StatusCode >= 400)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await ReadResponseBodyAsync(responseBodyStream);
                logger.BeginScope(new Dictionary<string, object>
                {
                    { "http.request.body", requestBody },
                    { "http.response.body", responseBody },
                });
                logger.LogWarning(requestBody);
                logger.LogWarning(responseBody);
                responseBodyStream.Seek(0, SeekOrigin.Begin);
            }

            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        var pipe = new Pipe();
        var sb = new StringBuilder();

        // Ensure each request gets a unique buffer
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            await request.Body.CopyToAsync(pipe.Writer);
            await pipe.Writer.CompleteAsync();

            var reader = pipe.Reader;
            while (true)
            {
                var result = await reader.ReadAsync();
                var sequence = result.Buffer;

                foreach (var segment in sequence) sb.Append(Encoding.UTF8.GetString(segment.Span));

                reader.AdvanceTo(sequence.End);
                if (result.IsCompleted)
                    break;
            }

            await reader.CompleteAsync();
            request.Body.Position = 0;
        }
        finally
        {
            // Return the buffer safely
            ArrayPool<byte>.Shared.Return(buffer, true);
        }

        return sb.ToString();
    }

    private async Task<string> ReadResponseBodyAsync(Stream responseBodyStream)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        var sb = new StringBuilder();

        try
        {
            int bytesRead;
            while ((bytesRead = await responseBodyStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, true);
        }

        return sb.ToString();
    }
}