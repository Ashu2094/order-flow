namespace OrderFlow.Payments.Api;

public class PaymentSimulationOptions
{
    public const string SectionName = "PaymentSimulation";

    public bool ForceFailure { get; set; }
}