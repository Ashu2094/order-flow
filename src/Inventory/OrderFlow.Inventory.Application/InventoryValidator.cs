
namespace OrderFlow.Inventory.Application;

public static class InventoryValidator
{
    public static Dictionary<string, string[]> ValidateCreate(
        CreateInventoryItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Sku))
            errors["sku"] = ["SKU is required."];
        else if (!IsValidSku(request.Sku))
            errors["sku"] = [
                "SKU can have at most 64 characters: letters, digits and '-' only."
            ];

        AddCommonErrors(
            errors, request.Name, request.Quantity, request.Price);

        return errors;
    }

    public static Dictionary<string, string[]> ValidateUpdate(
        UpdateInventoryItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        AddCommonErrors(
            errors, request.Name, request.Quantity, request.Price);

        return errors;
    }

    public static bool IsValidSku(string sku)
    {
        var clean = sku.Trim();

        return clean.Length <= 64 &&
               clean.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    private static void AddCommonErrors(
        Dictionary<string, string[]> errors,
        string? name,
        int quantity,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            errors["name"] = ["Name is required."];
        else if (name.Trim().Length > 200)
            errors["name"] = ["Name can have at most 200 characters."];

        if (quantity < 0)
            errors["quantity"] = ["Quantity cannot be negative."];

        if (price < 0)
            errors["price"] = ["Price cannot be negative."];
    }
}
