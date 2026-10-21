namespace OrderFlow.Contracts;

public record StockReserved(Guid OrderId);

public record StockReservationFailed(Guid OrderId, string Reason);