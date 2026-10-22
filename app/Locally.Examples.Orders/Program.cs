using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Locally.Examples.Orders;

var builder = WebApplication.CreateBuilder(args);

// App Service routes traffic to the container on the port in PORT.
builder.WebHost.UseUrls($"http://+:{builder.Configuration["PORT"] ?? "8080"}");

builder.Services.AddProblemDetails();

builder.Services.AddSingleton(_ => new BlobContainerClient(
    builder.Configuration.GetConnectionString("Storage"),
    builder.Configuration["Orders:Container"] ?? "orders"));

builder.Services.AddSingleton(_ => new ServiceBusClient(builder.Configuration.GetConnectionString("ServiceBus")));
builder.Services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClient>()
    .CreateSender(builder.Configuration["Orders:Queue"] ?? "orders"));

builder.Services.AddSingleton<OrderStore>();

var app = builder.Build();

// How long a request may spend talking to Storage and Service Bus before the API
// gives up and tells the caller, rather than leaving them waiting on SDK retries.
var budget = TimeSpan.FromSeconds(30);

app.MapGet("/healthz", () => "ok");

app.MapGet("/products", () => Products.All);

app.MapPost("/orders", async (OrderRequest? request, OrderStore store, HttpContext http) =>
{
    var (order, errors) = Order.From(request);
    if (order is null)
    {
        return Results.ValidationProblem(errors);
    }

    return await WithinBudget(http, async ct =>
    {
        await store.SaveAsync(order, ct);
        return Results.Created($"/orders/{order.Id}", order);
    });
});

app.MapGet("/orders/{id:guid}", (Guid id, OrderStore store, HttpContext http) =>
    WithinBudget(http, async ct => await store.GetAsync(id, ct) is { } order
        ? Results.Ok(order)
        : Results.NotFound()));

app.Run();

async Task<IResult> WithinBudget(HttpContext http, Func<CancellationToken, Task<IResult>> work)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
    cts.CancelAfter(budget);
    try
    {
        return await work(cts.Token);
    }
    catch (OperationCanceledException) when (!http.RequestAborted.IsCancellationRequested)
    {
        app.Logger.LogWarning("Request to {Path} timed out after {Budget}", http.Request.Path, budget);
        return Results.Problem(
            title: "The request timed out",
            detail: $"Storage or Service Bus didn't respond within {budget.TotalSeconds:0} seconds.",
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
}
