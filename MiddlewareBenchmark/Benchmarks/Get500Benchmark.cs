﻿using System.Text;
using BenchmarkDotNet.Attributes;
using HttpBodyMiddleware.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace MiddlewareBenchmark.Benchmarks;

[MemoryDiagnoser]
public class Get500Benchmark
{
    private readonly string BigString = new('a', 10240);
    private readonly string SmallString = new('a', 512);

    private DefaultHttpContext _context;
    private SimpleHttpContextLoggingMiddleware _httpContextLogging;
    private MemoryHttpContextLoggingMiddleware _memoryHttpContextLogging;
    private BalancedHttpContextLoggingMiddleware _speedHttpContextLogging;

    [Params(512, 10 * 1024)] public int BodySize; // 0.5 KB and 10 KB payloads

    [GlobalSetup]
    public void SetupContext()
    {
        var body = Encoding.UTF8.GetBytes(BodySize == 512 ? SmallString : BigString);

        _httpContextLogging = new SimpleHttpContextLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 500;
            return Task.CompletedTask;
        }, new Mock<ILogger<SimpleHttpContextLoggingMiddleware>>().Object);

        _memoryHttpContextLogging = new MemoryHttpContextLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 500;
            return Task.CompletedTask;
        }, new Mock<ILogger<MemoryHttpContextLoggingMiddleware>>().Object);

        _speedHttpContextLogging = new BalancedHttpContextLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 500;
            return Task.CompletedTask;
        }, new Mock<ILogger<BalancedHttpContextLoggingMiddleware>>().Object);

        _context = new DefaultHttpContext
        {
            Request =
            {
                Body = Stream.Null
            },
            Response =
            {
                Body = Stream.Null
            }
        };
    }

    [Benchmark(Baseline = true)]
    public async Task SimpleHttpContextLoggingMiddleware()
    {
        await _httpContextLogging.Invoke(_context);
    }

    [Benchmark]
    public async Task MemoryHttpContextLoggingMiddleware()
    {
        await _memoryHttpContextLogging.Invoke(_context);
    }

    [Benchmark]
    public async Task BalancedHttpContextLoggingMiddleware()
    {
        await _speedHttpContextLogging.Invoke(_context);
    }
}