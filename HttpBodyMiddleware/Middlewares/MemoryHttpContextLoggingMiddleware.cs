using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace HttpBodyMiddleware.Middlewares;

public class MemoryHttpContextLoggingMiddleware(
    RequestDelegate next,
    ILogger<MemoryHttpContextLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();

        // Read request body safely
        var requestBody = await ReadRequestBody(context.Request).ConfigureAwait(false);

        // Store original response body stream
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            if (context.Response.StatusCode >= 400)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await ReadResponseBodyAsync(responseBodyStream).ConfigureAwait(false);
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

    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        var pipe = new Pipe();
        var sb = new StringBuilder();

        // Ensure each request gets a unique buffer
        var buffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            await request.Body.CopyToAsync(pipe.Writer).ConfigureAwait(false);
            await pipe.Writer.CompleteAsync().ConfigureAwait(false);

            var reader = pipe.Reader;
            while (true)
            {
                var result = await reader.ReadAsync().ConfigureAwait(false);
                var sequence = result.Buffer;

                foreach (var segment in sequence) sb.Append(Encoding.UTF8.GetString(segment.Span));

                reader.AdvanceTo(sequence.End);
                if (result.IsCompleted)
                    break;
            }

            await reader.CompleteAsync().ConfigureAwait(false);
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