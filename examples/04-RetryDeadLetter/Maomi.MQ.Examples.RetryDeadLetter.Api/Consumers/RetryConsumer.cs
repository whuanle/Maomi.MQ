using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Examples.RetryDeadLetter.Api.Messages;

namespace Maomi.MQ.Examples.RetryDeadLetter.Api.Consumers;

[Consumer(
    "example.retry.main",
    Qos = 1,
    RetryFaildRequeue = false,
    DeadExchange = "",
    DeadRoutingKey = "example.retry.dead")]
public sealed class RetryConsumer : IConsumer<RetryMessage>
{
    private readonly ILogger<RetryConsumer> _logger;
    private readonly IMessagePublisher _publisher;

    public RetryConsumer(ILogger<RetryConsumer> logger, IMessagePublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, RetryMessage message)
    {
        _logger.LogInformation(
            "Retry consumer execute. HeaderId={HeaderId}, MessageId={MessageId}, ForceFail={ForceFail}",
            messageHeader.Id,
            message.Id,
            message.ForceFail);

        if (message.ForceFail)
        {
            throw new InvalidOperationException("This message is intentionally failed for retry/dead-letter demo.");
        }

        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, RetryMessage message)
    {
        _logger.LogWarning(
            ex,
            "Retry attempt failed. HeaderId={HeaderId}, RetryCount={RetryCount}, MessageId={MessageId}",
            messageHeader.Id,
            retryCount,
            message.Id);
        return Task.CompletedTask;
    }

    public async Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, RetryMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Retry fallback reached. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);

        if (message != null)
        {
            await _publisher.PublishAsync(
                exchange: string.Empty,
                routingKey: "example.retry.dead",
                message: new RetryDeadMessage
                {
                    Id = message.Id,
                    Text = message.Text,
                    DeadAt = DateTimeOffset.UtcNow
                });
        }

        return ConsumerState.Ack;
    }
}
