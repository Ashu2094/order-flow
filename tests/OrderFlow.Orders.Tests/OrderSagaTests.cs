using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts;
using OrderFlow.Orders.Api;
using OrderFlow.Orders.Infrastructure;
using System.Text;

namespace OrderFlow.Orders.Tests;

[Trait("Category", "Saga")]
public class OrderSagaTests : IAsyncLifetime
   // Gives each test a fresh harness (InitializeAsync runs before each test, DisposeAsync after)
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<OrderStateMachine, OrderState> _saga = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => x.AddSagaStateMachine<OrderStateMachine, OrderState>())
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
        _saga = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    // ---------- helpers: move the saga step by step ----------

    private async Task<Guid> SubmitOrderWithStockReserved()
    {
        var orderId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmitted(
            orderId, DateTime.UtcNow, [new OrderSubmittedItem("KB-001", 2)], 99.98m));
        Assert.NotNull(await _saga.Exists(orderId, s => s.AwaitingStock));

        await _harness.Bus.Publish(new StockReserved(orderId));
        Assert.NotNull(await _saga.Exists(orderId, s => s.AwaitingPayment));

        return orderId;
    }

    private async Task CancelAfterPaymentFailure(Guid orderId)
    {
        await _harness.Bus.Publish(new PaymentFailed(orderId, "Card declined"));
        Assert.NotNull(await _saga.Exists(orderId, s => s.Releasing));

        await _harness.Bus.Publish(new StockReleased(orderId));
        Assert.NotNull(await _saga.Exists(orderId, s => s.Cancelled));
    }

    // ---------- tests ----------

    [Fact]
    public async Task Happy_path_ends_Confirmed()
    {
        var orderId = await SubmitOrderWithStockReserved();

        Assert.True(await _harness.Sent.Any<ChargePayment>(m =>
            m.Context.Message.OrderId == orderId && m.Context.Message.Amount == 99.98m));

        await _harness.Bus.Publish(new PaymentCompleted(orderId));
        Assert.NotNull(await _saga.Exists(orderId, s => s.Confirmed));

        Assert.True(await _harness.Published.Any<OrderConfirmed>(m =>
            m.Context.Message.OrderId == orderId));
    }

    [Fact]
    public async Task Payment_failure_releases_stock_and_ends_Cancelled()
    {
        var orderId = await SubmitOrderWithStockReserved();
        await CancelAfterPaymentFailure(orderId);

        Assert.True(await _harness.Sent.Any<ReleaseStock>(m =>
            m.Context.Message.OrderId == orderId &&
            m.Context.Message.Items.Single().Sku == "KB-001" &&
            m.Context.Message.Items.Single().Quantity == 2));

        Assert.True(await _harness.Published.Any<OrderCancelled>(m =>
            m.Context.Message.OrderId == orderId &&
            m.Context.Message.Reason == "Card declined"));
    }

    [Fact]
    public async Task Late_PaymentCompleted_after_cancel_is_flagged_not_obeyed()
    {
        var orderId = await SubmitOrderWithStockReserved();
        await CancelAfterPaymentFailure(orderId);

        await _harness.Bus.Publish(new PaymentCompleted(orderId));
        Assert.True(await _saga.Consumed.Any<PaymentCompleted>(m =>
            m.Context.Message.OrderId == orderId));

        var instance = _saga.Sagas.Contains(orderId);
        Assert.NotNull(instance);
        Assert.Equal("Cancelled", instance.CurrentState);
        Assert.True(instance.NeedsRefund);
        Assert.False(await _harness.Published.Any<OrderConfirmed>());
    }
}