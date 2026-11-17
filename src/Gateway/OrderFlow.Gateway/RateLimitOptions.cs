namespace OrderFlow.Gateway;

public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 10;
}