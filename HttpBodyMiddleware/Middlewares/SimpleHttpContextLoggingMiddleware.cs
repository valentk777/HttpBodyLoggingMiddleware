using System.Text;

namespace HttpBodyMiddleware.Middlewares;

public class SimpleHttpContextLoggingMiddleware(
    RequestDelegate next,
    ILogger<SimpleHttpContextLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();
        var requestBody = await ReadRequestBody(context.Request).ConfigureAwait(false);

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
                var responseBody = await new StreamReader(responseBodyStream, Encoding.UTF8).ReadToEndAsync()
                    .ConfigureAwait(false);

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

    // Note: not thread safe
    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync().ConfigureAwait(false);
        request.Body.Position = 0;
        return body;
    }
}