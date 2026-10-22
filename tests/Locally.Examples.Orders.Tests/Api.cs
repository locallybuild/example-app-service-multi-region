using System.Net.Http.Json;
using System.Text.Json;

namespace Locally.Examples.Orders.Tests;

/// <summary>
/// The deployed orders API. Its URL comes from the ORDERS_API_URL environment
/// variable, which the README sets from the Terraform output.
/// </summary>
public static class Api
{
    private static readonly Lazy<HttpClient> LazyClient = new(() =>
    {
        var url = Environment.GetEnvironmentVariable("ORDERS_API_URL");
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException(
                "ORDERS_API_URL isn't set. From the repository root, run: " +
                "export ORDERS_API_URL=$(terraform -chdir=environments/locally output -raw api_url)");
        }

        return new HttpClient
        {
            BaseAddress = new Uri(url),

            // An order should be taken in a few seconds, not leave the customer waiting.
            Timeout = TimeSpan.FromSeconds(10),
        };
    });

    public static HttpClient Client => LazyClient.Value;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Task<HttpResponseMessage> PlaceOrderAsync(params (string ProductId, int Quantity)[] items) =>
        Client.PostAsJsonAsync("/orders", new
        {
            items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }),
        }, TestContext.Current.CancellationToken);
}

public record Product(string Id, string Name, decimal Price);

public record OrderLine(string ProductId, int Quantity);

public record Order(Guid Id, List<OrderLine> Items, decimal Total, DateTimeOffset CreatedAt);
