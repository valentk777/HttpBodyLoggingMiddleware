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

### **Observations:**

1. **Performance:**
    - *SimpleHttpContextLoggingMiddleware* generally has the lowest execution time in GET requests, especially with
      smaller payloads.
    - *MemoryHttpContextLoggingMiddleware* is slower but allocates significantly less memory.
    - *BalancedHttpContextLoggingMiddleware* tends to be slightly faster than *MemoryHttpContextLoggingMiddleware* and
      sometimes comparable to *SimpleHttpContextLoggingMiddleware*.

2. **Memory Allocation:**
    - *MemoryHttpContextLoggingMiddleware* consistently has the lowest memory allocation, often using less than a third
      of what the others allocate.
    - *SimpleHttpContextLoggingMiddleware* and *BalancedHttpContextLoggingMiddleware* allocate similar memory amounts,
      except in large payload POST requests, where *BalancedHttpContextLoggingMiddleware* is slightly better.

3. **Error Handling (500 Responses):**
    - When returning 500 errors, all middleware variants take significantly longer to execute.
    - *MemoryHttpContextLoggingMiddleware* has the highest execution time in some cases, making it less suitable for
      failure scenarios.

4. **Impact of Payload Size:**
    - With larger payloads (10,240 bytes), execution time increases significantly in POST requests but less so in GET
      requests.
    - Memory allocation scales up as expected, but *MemoryHttpContextLoggingMiddleware* continues to be more efficient
      in memory use.

### **Recommendations:**

- **Use *SimpleHttpContextLoggingMiddleware*** when execution speed is the highest priority and memory usage is not a
  concern. It performs best in low-latency scenarios.
- **Use *MemoryHttpContextLoggingMiddleware*** if reducing memory allocation is critical (e.g., in high-concurrency
  environments with limited resources). However, be mindful that it is generally slower.
- **Use *BalancedHttpContextLoggingMiddleware*** as a balanced choice when both execution speed and memory efficiency
  matter. It provides a middle ground without extreme trade-offs.

200 GET

| Method                               | PayloadSize |     Mean |     Error |   StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|--------------------------------------|-------------|---------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512         | 403.6 ns | 437.20 ns | 23.96 ns |  1.00 |    0.07 | 0.2174 | 0.0024 |    3640 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 581.5 ns | 159.90 ns |  8.76 ns |  1.44 |    0.08 | 0.0486 |      - |     816 B |        0.22 |
| BalancedHttpContextLoggingMiddleware | 512         | 464.3 ns | 462.88 ns | 25.37 ns |  1.15 |    0.08 | 0.2174 | 0.0024 |    3640 B |        1.00 |
| SimpleHttpContextLoggingMiddleware   | 10240       | 375.3 ns | 170.28 ns |  9.33 ns |  1.00 |    0.03 | 0.2174 | 0.0024 |    3640 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 568.5 ns |  18.66 ns |  1.02 ns |  1.52 |    0.03 | 0.0486 |      - |     816 B |        0.22 |
| BalancedHttpContextLoggingMiddleware | 10240       | 404.3 ns | 154.60 ns |  8.47 ns |  1.08 |    0.03 | 0.2174 | 0.0024 |    3640 B |        1.00 |

200 POST

| Method                               | PayloadSize |       Mean |       Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|--------------------------------------|-------------|-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512         |   588.1 ns |   304.61 ns |  16.70 ns |  1.00 |    0.04 | 0.3433 | 0.0057 |    5752 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         |   866.1 ns |   282.56 ns |  15.49 ns |  1.47 |    0.04 | 0.2375 |      - |    3976 B |        0.69 |
| BalancedHttpContextLoggingMiddleware | 512         |   586.5 ns |   257.92 ns |  14.14 ns |  1.00 |    0.03 | 0.3433 | 0.0057 |    5752 B |        1.00 |
| SimpleHttpContextLoggingMiddleware   | 10240       | 2,869.7 ns | 1,759.42 ns |  96.44 ns |  1.00 |    0.04 | 3.3951 | 0.5646 |   56856 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 3,537.4 ns | 2,759.28 ns | 151.25 ns |  1.23 |    0.06 | 4.4632 | 0.4921 |   74632 B |        1.31 |
| BalancedHttpContextLoggingMiddleware | 10240       | 2,987.4 ns | 1,901.15 ns | 104.21 ns |  1.04 |    0.04 | 3.3951 | 0.5646 |   56856 B |        1.00 |

500 GET

| Method                               | PayloadSize |       Mean |       Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|--------------------------------------|-------------|-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512         | 3,434.0 ns | 1,347.91 ns |  73.88 ns |  1.00 |    0.03 | 0.5417 | 0.1373 |    9152 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 3,906.4 ns | 3,805.65 ns | 208.60 ns |  1.14 |    0.06 | 0.1793 | 0.0877 |    3048 B |        0.33 |
| BalancedHttpContextLoggingMiddleware | 512         | 4,132.4 ns | 6,258.45 ns | 343.05 ns |  1.20 |    0.09 | 0.3510 | 0.1183 |    5872 B |        0.64 |
| SimpleHttpContextLoggingMiddleware   | 10240       | 3,213.6 ns | 1,918.97 ns | 105.19 ns |  1.00 |    0.04 | 0.5455 | 0.1373 |    9152 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 3,775.1 ns | 5,536.71 ns | 303.49 ns |  1.18 |    0.09 | 0.1755 | 0.0839 |    3048 B |        0.33 |
| BalancedHttpContextLoggingMiddleware | 10240       | 3,149.2 ns | 2,241.11 ns | 122.84 ns |  0.98 |    0.04 | 0.3510 | 0.1183 |    5872 B |        0.64 |

500 GET with result pattern response

| Method                               | PayloadSize |       Mean |       Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|--------------------------------------|-------------|-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512         | 3,447.1 ns | 2,310.95 ns | 126.67 ns |  1.00 |    0.04 | 0.5455 | 0.1373 |    9152 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 4,031.7 ns | 3,945.05 ns | 216.24 ns |  1.17 |    0.07 | 0.1793 | 0.0877 |    3048 B |        0.33 |
| BalancedHttpContextLoggingMiddleware | 512         | 3,704.3 ns | 5,173.24 ns | 283.56 ns |  1.08 |    0.08 | 0.3510 | 0.1183 |    5872 B |        0.64 |
| SimpleHttpContextLoggingMiddleware   | 10240       | 3,153.1 ns | 1,327.99 ns |  72.79 ns |  1.00 |    0.03 | 0.5455 | 0.1373 |    9152 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 3,302.2 ns | 3,243.22 ns | 177.77 ns |  1.05 |    0.05 | 0.1755 | 0.0839 |    3048 B |        0.33 |
| BalancedHttpContextLoggingMiddleware | 10240       | 3,169.1 ns | 1,643.66 ns |  90.09 ns |  1.01 |    0.03 | 0.3510 | 0.1144 |    5872 B |        0.64 |

500 POST

| Method                               | PayloadSize |        Mean |        Error |      StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|--------------------------------------|-------------|------------:|-------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512         |  4,811.6 ns |  6,913.36 ns |   378.94 ns |  1.00 |    0.10 | 0.6714 | 0.2213 |   11264 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         |  6,043.0 ns |  8,379.50 ns |   459.31 ns |  1.26 |    0.12 | 0.3662 | 0.1831 |    6208 B |        0.55 |
| BalancedHttpContextLoggingMiddleware | 512         |  5,429.2 ns |  2,978.10 ns |   163.24 ns |  1.13 |    0.08 | 0.4730 | 0.2365 |    7984 B |        0.71 |
| SimpleHttpContextLoggingMiddleware   | 10240       | 19,166.2 ns | 15,574.15 ns |   853.67 ns |  1.00 |    0.05 | 3.7231 | 1.8616 |   62369 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 18,994.8 ns | 22,217.11 ns | 1,217.79 ns |  0.99 |    0.07 | 4.5776 | 2.2888 |   76865 B |        1.23 |
| BalancedHttpContextLoggingMiddleware | 10240       | 16,790.4 ns |  7,616.05 ns |   417.46 ns |  0.88 |    0.04 | 3.5095 | 1.7395 |   59089 B |        0.95 |

500 POST with result pattern response

| Method                               | PayloadSize |        Mean |        Error |      StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|--------------------------------------|-------------|------------:|-------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512         |  4,729.2 ns |  7,272.78 ns |   398.65 ns |  1.00 |    0.10 | 0.6714 | 0.2213 |   11264 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         |  5,719.4 ns |  7,232.85 ns |   396.46 ns |  1.21 |    0.11 | 0.3662 | 0.1831 |    6208 B |        0.55 |
| BalancedHttpContextLoggingMiddleware | 512         |  4,466.7 ns |  3,403.87 ns |   186.58 ns |  0.95 |    0.08 | 0.4730 | 0.2365 |    7984 B |        0.71 |
| SimpleHttpContextLoggingMiddleware   | 10240       | 17,447.5 ns | 44,975.20 ns | 2,465.24 ns |  1.01 |    0.17 | 3.7231 | 1.8616 |   62369 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 18,625.2 ns | 33,581.24 ns | 1,840.70 ns |  1.08 |    0.15 | 4.5776 | 2.2888 |   76865 B |        1.23 |
| BalancedHttpContextLoggingMiddleware | 10240       | 16,750.6 ns | 14,972.44 ns |   820.69 ns |  0.97 |    0.12 | 3.5095 | 1.7395 |   59089 B |        0.95 |

## Full results

| Method                               | PayloadSize | StatusCode | WithRequestBody | WithResponseBody |        Mean |        Error |      StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|--------------------------------------|-------------|------------|-----------------|------------------|------------:|-------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SimpleHttpContextLoggingMiddleware   | 512         | 200        | False           | False            |    418.6 ns |    170.97 ns |     9.37 ns |  1.00 |    0.03 | 0.2174 | 0.0019 |    3640 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 200        | False           | False            |    573.0 ns |    192.88 ns |    10.57 ns |  1.37 |    0.03 | 0.0486 |      - |     816 B |        0.22 |
| BalancedHttpContextLoggingMiddleware | 512         | 200        | False           | False            |    444.9 ns |     87.80 ns |     4.81 ns |  1.06 |    0.02 | 0.2174 | 0.0024 |    3640 B |        1.00 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 512         | 200        | False           | True             |    403.6 ns |    437.20 ns |    23.96 ns |  1.00 |    0.07 | 0.2174 | 0.0024 |    3640 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 200        | False           | True             |    581.5 ns |    159.90 ns |     8.76 ns |  1.44 |    0.08 | 0.0486 |      - |     816 B |        0.22 |
| BalancedHttpContextLoggingMiddleware | 512         | 200        | False           | True             |    464.3 ns |    462.88 ns |    25.37 ns |  1.15 |    0.08 | 0.2174 | 0.0024 |    3640 B |        1.00 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 512         | 200        | True            | False            |    652.2 ns |    988.94 ns |    54.21 ns |  1.00 |    0.10 | 0.3433 | 0.0057 |    5752 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 200        | True            | False            |  1,037.7 ns |    320.93 ns |    17.59 ns |  1.60 |    0.11 | 0.2375 |      - |    3976 B |        0.69 |
| BalancedHttpContextLoggingMiddleware | 512         | 200        | True            | False            |    693.6 ns |  2,226.86 ns |   122.06 ns |  1.07 |    0.18 | 0.3433 | 0.0057 |    5752 B |        1.00 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 512         | 200        | True            | True             |    588.1 ns |    304.61 ns |    16.70 ns |  1.00 |    0.04 | 0.3433 | 0.0057 |    5752 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 200        | True            | True             |    866.1 ns |    282.56 ns |    15.49 ns |  1.47 |    0.04 | 0.2375 |      - |    3976 B |        0.69 |
| BalancedHttpContextLoggingMiddleware | 512         | 200        | True            | True             |    586.5 ns |    257.92 ns |    14.14 ns |  1.00 |    0.03 | 0.3433 | 0.0057 |    5752 B |        1.00 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 512         | 500        | False           | False            |  3,434.0 ns |  1,347.91 ns |    73.88 ns |  1.00 |    0.03 | 0.5417 | 0.1373 |    9152 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 500        | False           | False            |  3,906.4 ns |  3,805.65 ns |   208.60 ns |  1.14 |    0.06 | 0.1793 | 0.0877 |    3048 B |        0.33 |
| BalancedHttpContextLoggingMiddleware | 512         | 500        | False           | False            |  4,132.4 ns |  6,258.45 ns |   343.05 ns |  1.20 |    0.09 | 0.3510 | 0.1183 |    5872 B |        0.64 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 512         | 500        | False           | True             |  3,447.1 ns |  2,310.95 ns |   126.67 ns |  1.00 |    0.04 | 0.5455 | 0.1373 |    9152 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 500        | False           | True             |  4,031.7 ns |  3,945.05 ns |   216.24 ns |  1.17 |    0.07 | 0.1793 | 0.0877 |    3048 B |        0.33 |
| BalancedHttpContextLoggingMiddleware | 512         | 500        | False           | True             |  3,704.3 ns |  5,173.24 ns |   283.56 ns |  1.08 |    0.08 | 0.3510 | 0.1183 |    5872 B |        0.64 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 512         | 500        | True            | False            |  4,811.6 ns |  6,913.36 ns |   378.94 ns |  1.00 |    0.10 | 0.6714 | 0.2213 |   11264 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 500        | True            | False            |  6,043.0 ns |  8,379.50 ns |   459.31 ns |  1.26 |    0.12 | 0.3662 | 0.1831 |    6208 B |        0.55 |
| BalancedHttpContextLoggingMiddleware | 512         | 500        | True            | False            |  5,429.2 ns |  2,978.10 ns |   163.24 ns |  1.13 |    0.08 | 0.4730 | 0.2365 |    7984 B |        0.71 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 512         | 500        | True            | True             |  4,729.2 ns |  7,272.78 ns |   398.65 ns |  1.00 |    0.10 | 0.6714 | 0.2213 |   11264 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 512         | 500        | True            | True             |  5,719.4 ns |  7,232.85 ns |   396.46 ns |  1.21 |    0.11 | 0.3662 | 0.1831 |    6208 B |        0.55 |
| BalancedHttpContextLoggingMiddleware | 512         | 500        | True            | True             |  4,466.7 ns |  3,403.87 ns |   186.58 ns |  0.95 |    0.08 | 0.4730 | 0.2365 |    7984 B |        0.71 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240       | 200        | False           | False            |    390.6 ns |     62.94 ns |     3.45 ns |  1.00 |    0.01 | 0.2174 | 0.0024 |    3640 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 200        | False           | False            |    577.0 ns |     47.79 ns |     2.62 ns |  1.48 |    0.01 | 0.0486 |      - |     816 B |        0.22 |
| BalancedHttpContextLoggingMiddleware | 10240       | 200        | False           | False            |    412.0 ns |    335.34 ns |    18.38 ns |  1.05 |    0.04 | 0.2174 | 0.0024 |    3640 B |        1.00 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240       | 200        | False           | True             |    375.3 ns |    170.28 ns |     9.33 ns |  1.00 |    0.03 | 0.2174 | 0.0024 |    3640 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 200        | False           | True             |    568.5 ns |     18.66 ns |     1.02 ns |  1.52 |    0.03 | 0.0486 |      - |     816 B |        0.22 |
| BalancedHttpContextLoggingMiddleware | 10240       | 200        | False           | True             |    404.3 ns |    154.60 ns |     8.47 ns |  1.08 |    0.03 | 0.2174 | 0.0024 |    3640 B |        1.00 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240       | 200        | True            | False            |  3,081.8 ns |  3,879.70 ns |   212.66 ns |  1.00 |    0.08 | 3.3951 | 0.5646 |   56856 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 200        | True            | False            |  3,532.8 ns |  1,895.47 ns |   103.90 ns |  1.15 |    0.07 | 4.4632 | 0.4921 |   74632 B |        1.31 |
| BalancedHttpContextLoggingMiddleware | 10240       | 200        | True            | False            |  2,976.6 ns |    999.09 ns |    54.76 ns |  0.97 |    0.06 | 3.3951 | 0.5646 |   56856 B |        1.00 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240       | 200        | True            | True             |  2,869.7 ns |  1,759.42 ns |    96.44 ns |  1.00 |    0.04 | 3.3951 | 0.5646 |   56856 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 200        | True            | True             |  3,537.4 ns |  2,759.28 ns |   151.25 ns |  1.23 |    0.06 | 4.4632 | 0.4921 |   74632 B |        1.31 |
| BalancedHttpContextLoggingMiddleware | 10240       | 200        | True            | True             |  2,987.4 ns |  1,901.15 ns |   104.21 ns |  1.04 |    0.04 | 3.3951 | 0.5646 |   56856 B |        1.00 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240       | 500        | False           | False            |  3,213.6 ns |  1,918.97 ns |   105.19 ns |  1.00 |    0.04 | 0.5455 | 0.1373 |    9152 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 500        | False           | False            |  3,775.1 ns |  5,536.71 ns |   303.49 ns |  1.18 |    0.09 | 0.1755 | 0.0839 |    3048 B |        0.33 |
| BalancedHttpContextLoggingMiddleware | 10240       | 500        | False           | False            |  3,149.2 ns |  2,241.11 ns |   122.84 ns |  0.98 |    0.04 | 0.3510 | 0.1183 |    5872 B |        0.64 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240       | 500        | False           | True             |  3,153.1 ns |  1,327.99 ns |    72.79 ns |  1.00 |    0.03 | 0.5455 | 0.1373 |    9152 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 500        | False           | True             |  3,302.2 ns |  3,243.22 ns |   177.77 ns |  1.05 |    0.05 | 0.1755 | 0.0839 |    3048 B |        0.33 |
| BalancedHttpContextLoggingMiddleware | 10240       | 500        | False           | True             |  3,169.1 ns |  1,643.66 ns |    90.09 ns |  1.01 |    0.03 | 0.3510 | 0.1144 |    5872 B |        0.64 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240       | 500        | True            | False            | 19,166.2 ns | 15,574.15 ns |   853.67 ns |  1.00 |    0.05 | 3.7231 | 1.8616 |   62369 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 500        | True            | False            | 18,994.8 ns | 22,217.11 ns | 1,217.79 ns |  0.99 |    0.07 | 4.5776 | 2.2888 |   76865 B |        1.23 |
| BalancedHttpContextLoggingMiddleware | 10240       | 500        | True            | False            | 16,790.4 ns |  7,616.05 ns |   417.46 ns |  0.88 |    0.04 | 3.5095 | 1.7395 |   59089 B |        0.95 |
|                                      |             |            |                 |                  |             |              |             |       |         |        |        |           |             |
| SimpleHttpContextLoggingMiddleware   | 10240       | 500        | True            | True             | 17,447.5 ns | 44,975.20 ns | 2,465.24 ns |  1.01 |    0.17 | 3.7231 | 1.8616 |   62369 B |        1.00 |
| MemoryHttpContextLoggingMiddleware   | 10240       | 500        | True            | True             | 18,625.2 ns | 33,581.24 ns | 1,840.70 ns |  1.08 |    0.15 | 4.5776 | 2.2888 |   76865 B |        1.23 |
| BalancedHttpContextLoggingMiddleware | 10240       | 500        | True            | True             | 16,750.6 ns | 14,972.44 ns |   820.69 ns |  0.97 |    0.12 | 3.5095 | 1.7395 |   59089 B |        0.95 |