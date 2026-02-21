using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Examples.RetryDeadLetter.Api.Messages;

namespace Maomi.MQ.Examples.RetryDeadLetter.Api.Consumers;

[Consumer("example.retry.dead", Qos = 1)]
public sealed class RetryDeadConsumer : IConsumer<RetryDeadMessage>
{
    private readonly ILogger<RetryDeadConsumer> _logger;

    public RetryDeadConsumer(ILogger<RetryDeadConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, RetryDeadMessage message)
    {
        _logger.LogInformation(
            "Dead-letter consumed. HeaderId={HeaderId}, MessageId={MessageId}, DeadAt={DeadAt}, Text={Text}",
            messageHeader.Id,
            message.Id,
            message.DeadAt,
            message.Text);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, RetryDeadMessage message)
    {
        _logger.LogWarning(
            ex,
            "Dead-letter consume failed. HeaderId={HeaderId}, RetryCount={RetryCount}",
            messageHeader.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, RetryDeadMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Dead-letter fallback. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}
