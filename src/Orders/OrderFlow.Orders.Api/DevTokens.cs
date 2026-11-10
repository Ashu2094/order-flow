namespace OrderFlow.Orders.Api;

public record DevTokenRequest(string UserName, int? ExpiresInMinutes, string[]? Roles);
public record DevTokenResponse(string AccessToken, DateTime ExpiresAtUtc);