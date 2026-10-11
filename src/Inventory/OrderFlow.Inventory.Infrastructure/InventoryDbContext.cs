using Microsoft.EntityFrameworkCore;

namespace OrderFlow.Inventory.Infrastructure;

public class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
}
