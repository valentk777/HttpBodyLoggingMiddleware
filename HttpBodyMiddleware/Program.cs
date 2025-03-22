using HttpBodyMiddleware;
using HttpBodyMiddleware.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/get-ok", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
        .ToArray();
    return forecast;
}).WithName("get-ok");
app.MapGet("/get-error", () => { throw new NotImplementedException(); }).WithName("get-error");

app.MapPost("/post-ok", (WeatherForecast body) =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("post-ok");
app.MapPost("/post-error", (WeatherForecast body) => { throw new NotImplementedException(); })
    .WithName("post-error");

app.UseSwagger();
app.UseSwaggerUI();

// app.UseMiddleware<SimpleHttpContextLoggingMiddleware>();
// app.UseMiddleware<MemoryHttpContextLoggingMiddleware>();
app.UseMiddleware<BalancedHttpContextLoggingMiddleware>();

app.Run();

namespace HttpBodyMiddleware
{
    internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}