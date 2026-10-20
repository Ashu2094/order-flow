using MassTransit;
using MassTransit.Testing;
using OrderFlow.Contracts;

namespace OrderFlow.Inventory.Api;

public class TestMessageConsumer(ILogger<TestMessageConsumer> logger): IConsumer<TestMessage>
{
    public Task Consume(ConsumeContext<TestMessage> context)
    {
        logger.LogInformation("Received test message '{Text}' sent at {SentAtUtc}",context.Message.Text,context.Message.SentAtUtc);

        return Task.CompletedTask;
    }
}