using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderFlow.Orders.Infrastructure;

public class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.ToTable("order_states");

        entity.Property(x => x.CurrentState).HasMaxLength(64);

        entity.Property(x => x.Version).IsRowVersion();
    }
}