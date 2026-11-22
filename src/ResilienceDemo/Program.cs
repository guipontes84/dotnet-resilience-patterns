using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("unstable", client =>
{
    client.BaseAddress = new Uri("http://localhost:5080");
}).AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromMilliseconds(500);
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
});

var app = builder.Build();
var attempts = 0;

app.MapGet("/", () => Results.Ok(new
{
    project = "dotnet-resilience-patterns",
    endpoints = new[] { "/demo?mode=flaky", "/demo?mode=slow", "/demo?mode=down" }
}));

app.MapGet("/external", async (string mode, CancellationToken ct) =>
{
    var attempt = Interlocked.Increment(ref attempts);
    if (mode == "slow")
        await Task.Delay(TimeSpan.FromSeconds(5), ct);
    if (mode == "down" || (mode == "flaky" && attempt % 3 != 0))
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    return Results.Ok(new { status = "ok", attempt, mode });
});

app.MapGet("/demo", async (string? mode, IHttpClientFactory factory, ILogger<Program> logger, CancellationToken ct) =>
{
    mode ??= "flaky";
    if (mode is not ("flaky" or "slow" or "down" or "ok"))
        return Results.BadRequest(new { error = "Use flaky, slow, down ou ok." });

    try
    {
        var client = factory.CreateClient("unstable");
        using var response = await client.GetAsync($"/external?mode={mode}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation("Upstream respondeu {StatusCode}", response.StatusCode);
        return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException or OperationCanceledException)
    {
        logger.LogWarning(ex, "Falha de resiliência no modo {Mode}", mode);
        return Results.Json(new { error = ex.GetType().Name, message = ex.Message }, statusCode: 503);
    }
});

app.Run("http://localhost:5080");
