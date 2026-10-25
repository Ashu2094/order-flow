
using OrderFlow.Orders.Domain;

namespace OrderFlow.Orders.Tests;

public class OrderStatusTests
{
    private static Order NewOrder() =>
        Order.Create(Guid.NewGuid(), "Asha Rao", DateTime.UtcNow);

    [Fact]
    public void Cancel_WhenPending_CancelsAndReturnsTrue()
    {
        var order = NewOrder();

        var result = order.Cancel();

        Assert.True(result);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ReturnsFalse()
    {
        var order = NewOrder();
        order.Cancel();

        Assert.False(order.Cancel());
    }

    [Fact]
    public void Cancel_WhenConfirmed_ChangesNothing()
    {
        var order = NewOrder();
        order.Confirm();

        var result = order.Cancel();

        Assert.False(result);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WhenCancelled_ChangesNothing()
    {
        var order = NewOrder();
        order.Cancel();

        order.Confirm();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void MarkFailed_WhenConfirmed_ChangesNothing()
    {
        var order = NewOrder();
        order.Confirm();

        order.MarkFailed();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }
}
