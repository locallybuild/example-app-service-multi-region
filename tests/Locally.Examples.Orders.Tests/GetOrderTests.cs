using System.Net;
using System.Net.Http.Json;

namespace Locally.Examples.Orders.Tests;

public class GetOrderTests
{
    [Fact]
    public async Task PlaceThenGet_ReturnsSameOrder()
    {
        var placed = await Api.PlaceOrderAsync(("pain-au-chocolat", 3));
        var created = await placed.Content.ReadFromJsonAsync<Order>(Api.Json, TestContext.Current.CancellationToken);

        var fetched = await Api.Client.GetFromJsonAsync<Order>($"/orders/{created!.Id}", Api.Json, TestContext.Current.CancellationToken);

        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Total, fetched.Total);
        Assert.Equal(new OrderLine("pain-au-chocolat", 3), Assert.Single(fetched.Items));
    }

    [Fact]
    public async Task GetUnknownOrder_ReturnsNotFound()
    {
        var response = await Api.Client.GetAsync($"/orders/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
