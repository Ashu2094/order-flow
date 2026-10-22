namespace OrderFlow.Contracts;

public record ChargePayment(Guid OrderId, decimal Amount);

public record PaymentCompleted(Guid OrderId);

public record PaymentFailed(Guid OrderId, string Reason);