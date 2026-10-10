
using OrderFlow.Inventory.Application;

namespace OrderFlow.Inventory.Tests;

public class InventoryValidatorTests
{
    [Fact]
    public void Valid_request_has_no_errors()
    {
        // Arrange
        var request = new CreateInventoryItemRequest(
            "KB-001", "Keyboard", null, 10, 49.99m);

        // Act
        var errors = InventoryValidator.ValidateCreate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Empty_name_is_rejected()
    {
        var request = new CreateInventoryItemRequest(
            "KB-001", "", null, 10, 49.99m);

        var errors = InventoryValidator.ValidateCreate(request);

        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void Negative_quantity_is_rejected()
    {
        var request = new CreateInventoryItemRequest(
            "KB-001", "Keyboard", null, -3, 49.99m);

        var errors = InventoryValidator.ValidateCreate(request);

        Assert.True(errors.ContainsKey("quantity"));
    }

    [Fact]
    public void Sku_with_symbols_is_rejected()
    {
        var request = new CreateInventoryItemRequest(
            "bad sku!", "Keyboard", null, 10, 49.99m);

        var errors = InventoryValidator.ValidateCreate(request);

        Assert.True(errors.ContainsKey("sku"));
    }
}
