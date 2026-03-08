using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Examples.QuickStart.Api.Messages;

namespace Maomi.MQ.Examples.QuickStart.Api.Consumers;

[Consumer("example.quickstart")]
public sealed class QuickStartConsumer : IConsumer<QuickStartMessage>
{
    private readonly ILogger<QuickStartConsumer> _logger;

    public QuickStartConsumer(ILogger<QuickStartConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, QuickStartMessage message)
    {
        _logger.LogInformation(
            "Consumed quickstart message. HeaderId={HeaderId}, MessageId={MessageId}, Text={Text}",
            messageHeader.Id,
            message.Id,
            message.Text);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, QuickStartMessage message)
    {
        _logger.LogWarning(
            ex,
            "Quickstart consume failed. HeaderId={HeaderId}, RetryCount={RetryCount}",
            messageHeader.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, QuickStartMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Quickstart fallback. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}
