namespace OrderFlow.Contracts;

public record TestMessage(
    string Text,
    DateTime SentAtUtc
);