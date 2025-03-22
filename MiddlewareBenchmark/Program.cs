using BenchmarkDotNet.Running;
using MiddlewareBenchmark.Benchmarks;

BenchmarkRunner.Run<Get200Benchmark>();
BenchmarkRunner.Run<Get500Benchmark>();
BenchmarkRunner.Run<Get500AndResultPatternBenchmark>();
BenchmarkRunner.Run<Post200Benchmark>();
BenchmarkRunner.Run<Post500Benchmark>();
BenchmarkRunner.Run<Post500AndResultPatternBenchmark>();