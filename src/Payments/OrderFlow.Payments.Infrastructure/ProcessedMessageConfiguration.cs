
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Payments.Domain;

namespace OrderFlow.Payments.Infrastructure;

public class ProcessedMessageConfiguration
    : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(
        EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");

        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.MessageId)
            .ValueGeneratedNever();
    }
}
