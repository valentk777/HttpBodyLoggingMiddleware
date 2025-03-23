# Http Request and Response Body Logging Middleware

This repository contains two HTTP request logging middlewares that capture both the request and response bodies and add
them as new log scopes or messages. To avoid cluttering the logs with unnecessary data, the logging is triggered **only
** when the status code is **>= 400**. This threshold can easily be adjusted to meet different needs or requirements.

The project includes two middlewares (with the possibility of more in the future). One is a general-purpose middleware,
while the other is optimized for memory usage (leveraging `System.IO.Pipelines.Pipe` and `System.Buffers.ArrayPool`).
However, it's worth noting that the memory-optimized version is slightly slower when handling small HTTP bodies.

## Benchmark results

The benchmark results compare three different HTTP context logging middleware implementations (
*SimpleHttpContextLoggingMiddleware*, *MemoryHttpContextLoggingMiddleware*, and *BalancedHttpContextLoggingMiddleware*)
under various request scenarios (GET/POST, 200/500 responses, different payload sizes).

## Summary

- For small payloads, `BalancedHttpContextLoggingMiddleware` consistently achieves the best performance, with
  `SimpleHttpContextLoggingMiddleware` being slightly slower and `MemoryHttpContextLoggingMiddleware` trading speed for
  lower memory usage.
- For larger payloads, `BalancedHttpContextLoggingMiddleware` maintains its efficiency, often outperforming
  `SimpleHttpContextLoggingMiddleware`, while `MemoryHttpContextLoggingMiddleware` shows increased execution time but
  lower allocations in some cases.
- `MemoryHttpContextLoggingMiddleware` consistently allocates the least memory, but in some cases, it leads to a
  noticeable performance hit, making it a trade-off depending on the use case.
- The allocation differences are significant, with `MemoryHttpContextLoggingMiddleware` often reducing memory usage by
  up to 70% compared to `SimpleHttpContextLoggingMiddleware`, but at the cost of execution speed.
- While `BalancedHttpContextLoggingMiddleware` doesn’t always lead in raw speed, it provides a consistently strong
  balance between execution time and memory efficiency across different payload sizes.

## Detailed

### Scenario: 200 GET

The results show that SimpleHttpContextLoggingMiddleware performs the fastest with minimal memory allocation.
MemoryHttpContextLoggingMiddleware shows higher latency and lower memory allocation, while
BalancedHttpContextLoggingMiddleware offers a balance between response time and memory consumption.

| Method                               | BodySize |     Mean | Ratio | Allocated | Alloc Ratio |
|--------------------------------------|----------|---------:|------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512      | 405.2 ns |  1.00 |    3640 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512      | 556.2 ns |  1.38 |     816 B |        0.22 |
| BalancedHttpContextLoggingMiddleware | 512      | 397.6 ns |  0.98 |    3640 B |        1.00 |
|                                      |          |          |       |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240    | 380.4 ns |  1.00 |    3640 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240    | 572.8 ns |  1.51 |     816 B |        0.22 |
| BalancedHttpContextLoggingMiddleware | 10240    | 391.5 ns |  1.03 |    3640 B |        1.00 |

### Scenario: 200 POST

For 200 POST requests, the SimpleHttpContextLoggingMiddleware again shows the best performance in terms of response
time, but at the cost of higher memory allocation. MemoryHttpContextLoggingMiddleware demonstrates slower performance
with lower memory consumption, while BalancedHttpContextLoggingMiddleware achieves better balance between time and
memory usage compared to the other two.

| Method                               | BodySize |       Mean | Ratio | Allocated | Alloc Ratio |
|--------------------------------------|----------|-----------:|------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512      |   550.5 ns |  1.00 |   5.62 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512      |   831.6 ns |  1.51 |   3.88 KB |        0.69 |
| BalancedHttpContextLoggingMiddleware | 512      |   582.4 ns |  1.06 |   5.62 KB |        1.00 |
|                                      |          |            |       |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240    | 2,894.2 ns |  1.00 |  55.52 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240    | 3,551.6 ns |  1.23 |  72.88 KB |        1.31 |
| BalancedHttpContextLoggingMiddleware | 10240    | 2,837.3 ns |  0.98 |  55.52 KB |        1.00 |

### Scenario: 500 GET

The 500 GET scenario highlights the same patterns, with SimpleHttpContextLoggingMiddleware having the fastest response
time but with higher memory usage. MemoryHttpContextLoggingMiddleware offers better memory efficiency, but at a
significant cost in response time. BalancedHttpContextLoggingMiddleware continues to offer a middle ground, performing
almost as well as the simple method in terms of time while consuming slightly less memory.

| Method                               | BodySize |     Mean | Ratio | Allocated | Alloc Ratio |
|--------------------------------------|----------|---------:|------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512      | 3.118 us |  1.00 |   8.94 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512      | 3.113 us |  1.00 |   2.98 KB |        0.33 |
| BalancedHttpContextLoggingMiddleware | 512      | 3.042 us |  0.98 |   5.73 KB |        0.64 |
|                                      |          |          |       |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240    | 3.060 us |  1.00 |   8.94 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240    | 3.466 us |  1.13 |   2.98 KB |        0.33 |
| BalancedHttpContextLoggingMiddleware | 10240    | 3.005 us |  0.98 |   5.73 KB |        0.64 |

### Scenario: 500 GET with result pattern response

In this case, the performance differences are similar to the previous GET scenario. SimpleHttpContextLoggingMiddleware
remains the fastest, though BalancedHttpContextLoggingMiddleware is very close, offering slightly reduced memory usage.
MemoryHttpContextLoggingMiddleware has the worst performance in both time and memory efficiency.

| Method                               | BodySize |     Mean | Ratio | Allocated | Alloc Ratio |
|--------------------------------------|----------|---------:|------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512      | 3.063 us |  1.00 |   8.94 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512      | 3.171 us |  1.04 |   2.98 KB |        0.33 |
| BalancedHttpContextLoggingMiddleware | 512      | 3.020 us |  0.99 |   5.73 KB |        0.64 |
|                                      |          |          |       |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240    | 3.030 us |  1.00 |   8.94 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240    | 3.144 us |  1.04 |   2.98 KB |        0.33 |
| BalancedHttpContextLoggingMiddleware | 10240    | 2.950 us |  0.97 |   5.73 KB |        0.64 |

### Scenario: 500 POST

For 500 POST requests, the response times are significantly higher compared to GET requests, with the same trends
observed. SimpleHttpContextLoggingMiddleware has the lowest response time but requires the most memory, whereas
MemoryHttpContextLoggingMiddleware is more memory-efficient but slower. BalancedHttpContextLoggingMiddleware performs
similarly to SimpleHttpContextLoggingMiddleware in terms of time but with a more efficient memory allocation.

| Method                               | BodySize |      Mean | Ratio | Allocated | Alloc Ratio |
|--------------------------------------|----------|----------:|------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512      |  4.103 us |  1.00 |     11 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512      |  4.675 us |  1.14 |   6.06 KB |        0.55 |
| BalancedHttpContextLoggingMiddleware | 512      |  4.065 us |  0.99 |    7.8 KB |        0.71 |
|                                      |          |           |       |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240    | 16.222 us |  1.00 |  60.91 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240    | 17.878 us |  1.10 |  75.06 KB |        1.23 |
| BalancedHttpContextLoggingMiddleware | 10240    | 15.904 us |  0.98 |   57.7 KB |        0.95 |

### Scenario: 500 POST with result pattern response

The result-pattern response shows a slight advantage for SimpleHttpContextLoggingMiddleware, which is faster, but less
memory-efficient that it is competitors.

| Method                               | BodySize |      Mean | Ratio | Allocated | Alloc Ratio |
|--------------------------------------|----------|----------:|------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512      |  4.042 us |  1.00 |     11 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512      |  5.684 us |  1.41 |   6.06 KB |        0.55 |
| BalancedHttpContextLoggingMiddleware | 512      |  7.328 us |  1.81 |    7.8 KB |        0.71 |
|                                      |          |           |       |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240    | 23.843 us |  1.04 |  60.91 KB |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240    | 26.041 us |  1.13 |  75.06 KB |        1.23 |
| BalancedHttpContextLoggingMiddleware | 10240    | 24.451 us |  1.06 |   57.7 KB |        0.95 |
