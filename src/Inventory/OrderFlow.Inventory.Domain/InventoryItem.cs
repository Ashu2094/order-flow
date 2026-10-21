
using System.Runtime.Intrinsics.Arm;
using System.Threading.Channels;

namespace OrderFlow.Inventory.Domain;

public class InventoryItem
{
    private InventoryItem() { }

    public Guid Id { get; private set; }

    public string Sku { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public int Quantity { get; private set; }

    public decimal Price { get; private set; }

    public static InventoryItem Create(Guid id, string sku, string name, int quantity, decimal price, string? description = null)
    {
        return new InventoryItem
        {
            Id = id,
            Sku = sku,
            Name = name,
            Quantity = quantity,
            Price = price,
            Description = description
        };
    }

    public void Update(string name, string? description, int quantity, decimal price)
    {
        Name = name;
        Description = description;
        Quantity = quantity;
        Price = price;
    }

    public bool TryReserve(int quantity)
    {
        //Important: this method changes the in-memory entity.The Inventory consumer will save the change to the database later.
        if (quantity <= 0 || quantity > Quantity)
            return false;

        Quantity -= quantity;
        return true;
    }

}
