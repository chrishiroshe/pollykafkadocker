var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/payments/{orderId:guid}", async (
    Guid orderId,
    string? mode,
    CancellationToken ct) =>
{
    switch (mode?.ToLowerInvariant())
    {
        case "fail":
            return Results.Problem(
                title: "Payment failed",
                detail: $"Forced failure for order {orderId}",
                statusCode: 500);

        case "slow":
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return Results.Ok(new
            {
                orderId,
                status = "paid",
                processedAt = DateTime.UtcNow,
                mode = "slow"
            });

        case "random":
            await Task.Delay(500, ct);

            var success = Random.Shared.Next(1, 11) <= 7;
            if (!success)
            {
                return Results.Problem(
                    title: "Payment failed",
                    detail: $"Random failure for order {orderId}",
                    statusCode: 500);
            }

            return Results.Ok(new
            {
                orderId,
                status = "paid",
                processedAt = DateTime.UtcNow,
                mode = "random"
            });

        default:
            await Task.Delay(300, ct);
            return Results.Ok(new
            {
                orderId,
                status = "paid",
                processedAt = DateTime.UtcNow,
                mode = "success"
            });
    }
});

app.Run();

public partial class Program { }