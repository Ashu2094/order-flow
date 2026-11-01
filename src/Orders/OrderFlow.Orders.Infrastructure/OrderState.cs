using MassTransit;

namespace OrderFlow.Orders.Infrastructure;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } // Identifies the saga instance; here, it matches the order ID.

    public string CurrentState { get; set; } = string.Empty; //Stores the state name, such as AwaitingPayment.

    public decimal Amount { get; set; } //Stores the total to charge.

    public DateTime CreatedAtUtc { get; set; } //	Records when the saga was created.

    public uint Version { get; set; } //Supports optimistic concurrency using PostgreSQL's xmin mechanism.
}