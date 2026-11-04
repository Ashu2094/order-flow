using System.Text.Json;
using MassTransit;
using OrderFlow.Contracts;
using OrderFlow.Orders.Infrastructure;

namespace OrderFlow.Orders.Api;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State AwaitingStock { get; private set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;
    public State Releasing { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<OrderSubmitted> OrderSubmitted { get; private set; } = null!;
    public Event<StockReserved> StockReserved { get; private set; } = null!;
    public Event<StockReservationFailed> StockReservationFailed { get; private set; } = null!;
    public Event<PaymentCompleted> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailed { get; private set; } = null!;
    public Event<StockReleased> StockReleased { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockReserved, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockReservationFailed, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentCompleted, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentFailed, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockReleased, x => x.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderSubmitted)
                .Then(context =>
                {
                    context.Saga.Amount = context.Message.Total;
                    context.Saga.ItemsJson = JsonSerializer.Serialize(context.Message.Items);
                    context.Saga.CreatedAtUtc = DateTime.UtcNow;
                })
                .TransitionTo(AwaitingStock));

        During(AwaitingStock,
            When(StockReserved)
                .Send(new Uri("queue:ChargePayment"),
                    context => new ChargePayment(context.Saga.CorrelationId, context.Saga.Amount))
                .TransitionTo(AwaitingPayment),
            When(StockReservationFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .Publish(context => new OrderFailed(context.Saga.CorrelationId, context.Message.Reason))
                .TransitionTo(Failed));

        During(AwaitingPayment,
            When(PaymentCompleted)
                .Publish(context => new OrderConfirmed(context.Saga.CorrelationId))
                .TransitionTo(Confirmed),
            When(PaymentFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .Send(new Uri("queue:ReleaseStock"),
                    context => new ReleaseStock(context.Saga.CorrelationId, ReadItems(context.Saga)))
                .TransitionTo(Releasing));

        During(Releasing,
            When(StockReleased)
                .Publish(context => new OrderCancelled(context.Saga.CorrelationId,
                    context.Saga.FailureReason ?? "Payment failed."))
                .TransitionTo(Cancelled));

        // A cancelled order can never become Confirmed: warn and record it.
        During(Releasing, Cancelled,
            When(PaymentCompleted)
                .Then(context =>
                {
                    context.Saga.NeedsRefund = true;
                    LogContext.Warning?.Log(
                        "Late PaymentCompleted for order {OrderId} in state {State}; marked NeedsRefund",
                        context.Saga.CorrelationId, context.Saga.CurrentState);
                }));

        // Duplicates and late events that are harmless: ignore.
        During(AwaitingStock, AwaitingPayment, Releasing, Ignore(OrderSubmitted));
        During(Confirmed, Cancelled, Failed, Ignore(OrderSubmitted));
        During(AwaitingPayment, Releasing, Confirmed, Ignore(StockReserved), Ignore(StockReservationFailed));
        During(Cancelled, Failed, Ignore(StockReserved), Ignore(StockReservationFailed));
        During(Confirmed, Ignore(PaymentCompleted));
        During(Releasing, Cancelled, Confirmed, Ignore(PaymentFailed));
        During(Cancelled, Ignore(StockReleased));
    }

    private static List<OrderSubmittedItem> ReadItems(OrderState saga) =>
        JsonSerializer.Deserialize<List<OrderSubmittedItem>>(saga.ItemsJson) ?? [];
}