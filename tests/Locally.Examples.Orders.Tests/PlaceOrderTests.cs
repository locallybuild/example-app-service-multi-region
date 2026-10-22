using System.Net;
using System.Net.Http.Json;

namespace Locally.Examples.Orders.Tests;

public class PlaceOrderTests
{
    [Fact]
    public async Task PlaceOrder_ReturnsCreated()
    {
        var response = await Api.PlaceOrderAsync(("flat-white", 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<Order>(Api.Json, TestContext.Current.CancellationToken);
        Assert.Equal($"/orders/{order!.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PlaceOrder_CalculatesTotal()
    {
        var response = await Api.PlaceOrderAsync(("espresso", 2), ("croissant", 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<Order>(Api.Json, TestContext.Current.CancellationToken);
        Assert.Equal(6.90m, order!.Total);
    }

    [Fact]
    public async Task PlacedOrders_HaveUniqueIds()
    {
        var first = await Api.PlaceOrderAsync(("croissant", 1));
        var second = await Api.PlaceOrderAsync(("croissant", 1));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var a = await first.Content.ReadFromJsonAsync<Order>(Api.Json, TestContext.Current.CancellationToken);
        var b = await second.Content.ReadFromJsonAsync<Order>(Api.Json, TestContext.Current.CancellationToken);
        Assert.NotEqual(a!.Id, b!.Id);
    }
}
