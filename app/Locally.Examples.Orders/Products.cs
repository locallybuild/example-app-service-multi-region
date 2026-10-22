namespace Locally.Examples.Orders;

public record Product(string Id, string Name, decimal Price);

/// <summary>
/// The catalogue is fixed and held in memory, so listing it never leaves the app.
/// </summary>
public static class Products
{
    public static readonly IReadOnlyList<Product> All =
    [
        new("espresso", "Espresso", 2.40m),
        new("flat-white", "Flat White", 3.20m),
        new("croissant", "Croissant", 2.10m),
        new("pain-au-chocolat", "Pain au Chocolat", 2.60m),
    ];

    private static readonly Dictionary<string, Product> ById = All.ToDictionary(p => p.Id);

    public static Product? Find(string id) => ById.GetValueOrDefault(id);
}
