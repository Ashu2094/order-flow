using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Orders.Api;

public class InventoryServiceOptions //InventoryServiceOptions represents the InventoryService configuration section.
{
    public const string SectionName = "InventoryService";

    [Required, Url]
    public string BaseUrl { get; set; } = default!;
}