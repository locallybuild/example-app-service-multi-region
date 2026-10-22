using System.Net;
using System.Text;

namespace Locally.Examples.Orders.Tests;

public class OrderValidationTests
{
    [Fact]
    public async Task EmptyOrder_IsRejected()
    {
        var response = await Api.PlaceOrderAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownProduct_IsRejected()
    {
        var response = await Api.PlaceOrderAsync(("cappuccino", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Unknown product 'cappuccino'", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ZeroQuantity_IsRejected()
    {
        var response = await Api.PlaceOrderAsync(("espresso", 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Quantity must be at least 1", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MissingBody_IsRejected()
    {
        var content = new StringContent("", Encoding.UTF8, "application/json");

        var response = await Api.Client.PostAsync("/orders", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
