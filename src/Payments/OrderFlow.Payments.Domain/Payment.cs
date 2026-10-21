namespace OrderFlow.Payments.Domain;

public class Payment
{
    private Payment()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Payment Create(
        Guid id,
        Guid orderId,
        decimal amount,
        DateTime createdAtUtc)
    {
        return new Payment
        {
            Id = id,
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Pending,
            CreatedAtUtc = createdAtUtc
        };
    }
}