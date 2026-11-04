namespace OrderFlow.Contracts;

public record OrderCancelled(Guid OrderId, string Reason);
public record OrderFailed(Guid OrderId, string Reason);