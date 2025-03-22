using System.Text;

namespace HttpBodyMiddleware.Middlewares;

public class HttpContextLoggingMiddleware(RequestDelegate next, ILogger<HttpContextLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();
        var requestBody = await ReadRequestBody(context.Request);

        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await next(context);
        }
        finally
        {
            if (context.Response.StatusCode >= 400)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream, Encoding.UTF8).ReadToEndAsync();

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

    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }
}