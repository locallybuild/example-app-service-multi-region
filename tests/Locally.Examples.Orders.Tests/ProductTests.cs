using System.Net.Http.Json;

namespace Locally.Examples.Orders.Tests;

public class ProductTests
{
    [Fact]
    public async Task Products_ReturnsCatalogue()
    {
        var products = await GetProductsAsync();

        Assert.Equal(["espresso", "flat-white", "croissant", "pain-au-chocolat"], products.Select(p => p.Id));
    }

    [Fact]
    public async Task Products_HavePositivePrices()
    {
        var products = await GetProductsAsync();

        Assert.All(products, p => Assert.True(p.Price > 0, $"{p.Id} costs {p.Price}"));
    }

    private static async Task<List<Product>> GetProductsAsync() =>
        await Api.Client.GetFromJsonAsync<List<Product>>("/products", Api.Json, TestContext.Current.CancellationToken)
        ?? throw new InvalidOperationException("No products returned");
}
