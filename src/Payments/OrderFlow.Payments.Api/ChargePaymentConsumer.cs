using MassTransit;
using Microsoft.Extensions.Options;
using OrderFlow.Contracts;
using OrderFlow.Payments.Domain;
using OrderFlow.Payments.Infrastructure;

namespace OrderFlow.Payments.Api;

public class ChargePaymentConsumer(PaymentsDbContext db, IOptions<PaymentSimulationOptions> options, ILogger<ChargePaymentConsumer> logger) : IConsumer<ChargePayment>
{
    public async Task Consume(ConsumeContext<ChargePayment> context)
    {
        var message = context.Message;

        var payment = Payment.Create(
            Guid.NewGuid(),
            message.OrderId,
            message.Amount,
            DateTime.UtcNow
        );

        db.Payments.Add(payment);

        if (options.Value.ForceFailure)
        {
            payment.MarkFailed();

            await db.SaveChangesAsync(context.CancellationToken);

            logger.LogWarning(
                "Payment for order {OrderId} failed (simulated)",
                message.OrderId);

            await context.Publish(
                new PaymentFailed(
                    message.OrderId,
                    "Simulated payment failure."),
                context.CancellationToken);

            return;
        }

        payment.MarkSucceeded();

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Payment of {Amount} for order {OrderId} completed",
            message.Amount,
            message.OrderId);

        await context.Publish(
            new PaymentCompleted(message.OrderId),
            context.CancellationToken);
    }
}