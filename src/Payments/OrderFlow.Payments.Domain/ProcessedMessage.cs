
namespace OrderFlow.Payments.Domain;

public class ProcessedMessage
{
    private ProcessedMessage()
    {
    }

    public Guid MessageId { get; private set; }

    public DateTime ProcessedAtUtc { get; private set; }

    public static ProcessedMessage Create(
        Guid messageId,
        DateTime processedAtUtc)
    {
        return new ProcessedMessage
        {
            MessageId = messageId,
            ProcessedAtUtc = processedAtUtc
        };
    }
}
