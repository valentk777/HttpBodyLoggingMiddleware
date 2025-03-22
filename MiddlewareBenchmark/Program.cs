using BenchmarkDotNet.Running;

var summary = BenchmarkRunner.Run<MiddlewareBenchmark.MiddlewareBenchmark>();