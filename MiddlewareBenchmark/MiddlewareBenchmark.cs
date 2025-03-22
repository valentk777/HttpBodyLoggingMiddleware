using System.Text;
using BenchmarkDotNet.Attributes;
using HttpBodyMiddleware.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace MiddlewareBenchmark;

[MemoryDiagnoser]
[ShortRunJob]
public class MiddlewareBenchmark
{
    private HttpContextLoggingMiddleware _httpContextLogging;
    private MemoryHttpContextLoggingMiddleware _memoryHttpContextLogging;
    private SpeedHttpContextLoggingMiddleware _speedHttpContextLogging;
    private DefaultHttpContext _context;

    private string SmallString = new string('a', 512);
    private string BigString = new string('a', 10240);
    
    [Params(512, 10 * 1024)] public int PayloadSize; // 0.5 KB and 10 KB payloads

    [Params(200, 500)] public int StatusCode;

    [ParamsAllValues] public bool WithRequestBody;

    [ParamsAllValues] public bool WithResponseBody;

    [GlobalSetup]
    public void SetupContext()
    {
        var body = Encoding.UTF8.GetBytes(PayloadSize == 512 ? SmallString : BigString);

        _httpContextLogging = new HttpContextLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = StatusCode;
            return Task.CompletedTask;
        }, new Mock<ILogger<HttpContextLoggingMiddleware>>().Object);

        _memoryHttpContextLogging = new MemoryHttpContextLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = StatusCode;
            return Task.CompletedTask;
        }, new Mock<ILogger<MemoryHttpContextLoggingMiddleware>>().Object);

        _speedHttpContextLogging = new SpeedHttpContextLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = StatusCode;
            return Task.CompletedTask;
        }, new Mock<ILogger<SpeedHttpContextLoggingMiddleware>>().Object);

        _context = new DefaultHttpContext
        {
            Request =
            {
                Body = WithRequestBody
                    ? new MemoryStream(body)
                    : Stream.Null
            },
            Response =
            {
                Body = new MemoryStream()
            }
        };


        if (!WithResponseBody)
            return;

        _context.Response.Body.Write(body);
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
    }

    [Benchmark(Baseline = true)]
    public async Task HttpContextLoggingMiddleware() => await _httpContextLogging.Invoke(_context);

    [Benchmark]
    public async Task MemoryHttpContextLoggingMiddleware() => await _memoryHttpContextLogging.Invoke(_context);

    // [Benchmark]
    // public async Task SpeedHttpContextLoggingMiddleware() => await _speedHttpContextLogging.Invoke(_context);
}