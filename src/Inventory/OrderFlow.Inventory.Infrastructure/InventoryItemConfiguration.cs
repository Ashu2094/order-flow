using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Inventory.Domain;

namespace OrderFlow.Inventory.Infrastructure;

public class InventoryItemConfiguration
    : IEntityTypeConfiguration<InventoryItem>
{
    //InventoryItemConfiguration tell EF Core how C# InventoryItem class should be represented in the database.
    //IEntityTypeConfiguration => is an EF Core interface that lets you define database mapping rules. And <InventoryItem> tells EF Core which entity these rules apply to.
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items"); //ToTable sets the table name.

        builder.HasKey(i => i.Id); //HasKey makes Id the primary key.

        builder.Property(i => i.Id).ValueGeneratedNever(); //ValueGeneratedNever means your code supplies the ID.

        builder.Property(i => i.Sku)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Price)
            .HasPrecision(18, 2);

        builder.HasIndex(i => i.Sku).IsUnique();
    }
}