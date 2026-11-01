using MassTransit;
using OrderFlow.Contracts;
using OrderFlow.Orders.Infrastructure;

namespace OrderFlow.Orders.Api;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State AwaitingStock { get; private set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;

    public Event<OrderSubmitted> OrderSubmitted { get; private set; } = null!;
    public Event<StockReserved> StockReserved { get; private set; } = null!;
    public Event<PaymentCompleted> PaymentCompleted { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockReserved, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentCompleted, x => x.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderSubmitted)
                .Then(context =>
                {
                    context.Saga.Amount = context.Message.Total;
                    context.Saga.CreatedAtUtc = DateTime.UtcNow;
                })
                .TransitionTo(AwaitingStock));

        During(AwaitingStock,
            When(StockReserved)
                .Send(new Uri("queue:ChargePayment"),
                    context => new ChargePayment(context.Saga.CorrelationId, context.Saga.Amount))
                .TransitionTo(AwaitingPayment));

        During(AwaitingPayment,
            When(PaymentCompleted)
                .Publish(context => new OrderConfirmed(context.Saga.CorrelationId))
                .TransitionTo(Confirmed));

        During(AwaitingStock, AwaitingPayment, Confirmed, Ignore(OrderSubmitted));
        During(AwaitingPayment, Confirmed, Ignore(StockReserved));
        During(Confirmed, Ignore(PaymentCompleted));
    }
}