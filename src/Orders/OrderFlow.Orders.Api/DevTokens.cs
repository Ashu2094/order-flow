namespace OrderFlow.Orders.Api;

public record DevTokenRequest(string UserName, int? ExpiresInMinutes);
public record DevTokenResponse(string AccessToken, DateTime ExpiresAtUtc);