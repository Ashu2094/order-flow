namespace OrderFlow.Orders.Application;

public static class OrderValidator
{
    public static Dictionary<string, string[]> ValidateCreate(
        CreateOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            errors["customerName"] = ["Customer name is required."];
        }
        else if (request.CustomerName.Trim().Length > 200)
        {
            errors["customerName"] =
                ["Customer name can have at most 200 characters."];
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            errors["items"] = ["An order needs at least one item."];
            return errors;
        }

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];

            if (string.IsNullOrWhiteSpace(item.Sku) ||
                item.Sku.Trim().Length > 64)
            {
                errors[$"items[{i}].sku"] =
                    ["SKU is required and can have at most 64 characters."];
            }

            if (item.Quantity <= 0)
            {
                errors[$"items[{i}].quantity"] =
                    ["Quantity must be greater than 0."];
            }

            if (item.UnitPrice < 0)
            {
                errors[$"items[{i}].unitPrice"] =
                    ["Unit price cannot be negative."];
            }
        }

        return errors;
    }
}