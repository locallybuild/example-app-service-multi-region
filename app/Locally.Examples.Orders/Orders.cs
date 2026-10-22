using System.Text.Json;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;

namespace Locally.Examples.Orders;

public record OrderLine(string ProductId, int Quantity);

public record OrderRequest(List<OrderLine>? Items);

public record Order(Guid Id, IReadOnlyList<OrderLine> Items, decimal Total, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Builds an order from a request, or returns the validation errors that stop it.
    /// </summary>
    public static (Order? Order, Dictionary<string, string[]> Errors) From(OrderRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request?.Items is not { Count: > 0 } items)
        {
            errors["items"] = ["An order needs at least one item."];
            return (null, errors);
        }

        decimal total = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var product = Products.Find(items[i].ProductId ?? "");
            if (product is null)
            {
                errors[$"items[{i}].productId"] = [$"Unknown product '{items[i].ProductId}'."];
            }
            if (items[i].Quantity < 1)
            {
                errors[$"items[{i}].quantity"] = ["Quantity must be at least 1."];
            }
            total += (product?.Price ?? 0) * items[i].Quantity;
        }

        return errors.Count > 0
            ? (null, errors)
            : (new Order(Guid.NewGuid(), items, total, DateTimeOffset.UtcNow), errors);
    }
}

/// <summary>
/// Each order is written as a JSON blob, then announced on the Service Bus queue so
/// anything downstream (fulfilment, email) can pick it up.
/// </summary>
public class OrderStore(BlobContainerClient container, ServiceBusSender sender)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(Order order, CancellationToken cancellationToken)
    {
        var body = BinaryData.FromObjectAsJson(order, Json);

        await container.GetBlobClient($"{order.Id}.json").UploadAsync(body, cancellationToken);

        var message = new ServiceBusMessage(body)
        {
            MessageId = order.Id.ToString(),
            Subject = "order-placed",
            ContentType = "application/json",
        };
        await sender.SendMessageAsync(message, cancellationToken);
    }

    public async Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await container.GetBlobClient($"{id}.json").DownloadContentAsync(cancellationToken);
            return result.Value.Content.ToObjectFromJson<Order>(Json);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
