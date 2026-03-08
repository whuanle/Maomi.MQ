using Maomi.MQ;
using Maomi.MQ.Attributes;

namespace Maomi.MQ.Samples.LoadTest;

[Consumer(LoadTestRoutes.Json, Qos = 1000)]
public sealed class JsonLoadConsumer : IConsumer<JsonLoadMessage>
{
    private readonly ILogger<JsonLoadConsumer> _logger;
    private static long _count;

    public JsonLoadConsumer(ILogger<JsonLoadConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, JsonLoadMessage message)
    {
        var current = Interlocked.Increment(ref _count);
        if (current % 1000 == 0)
        {
            _logger.LogInformation("JSON consumer processed {Count} messages.", current);
        }

        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, JsonLoadMessage message)
    {
        _logger.LogWarning(ex, "JSON consumer failed. RetryCount={RetryCount}", retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, JsonLoadMessage? message, Exception? ex)
    {
        _logger.LogError(ex, "JSON consumer fallback.");
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer(LoadTestRoutes.ProtobufNet, Qos = 1000)]
public sealed class ProtobufNetLoadConsumer : IConsumer<ProtobufNetLoadMessage>
{
    private readonly ILogger<ProtobufNetLoadConsumer> _logger;
    private static long _count;

    public ProtobufNetLoadConsumer(ILogger<ProtobufNetLoadConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, ProtobufNetLoadMessage message)
    {
        var current = Interlocked.Increment(ref _count);
        if (current % 1_000 == 0)
        {
            _logger.LogInformation("Protobuf-net consumer processed {Count} messages.", current);
        }

        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, ProtobufNetLoadMessage message)
    {
        _logger.LogWarning(ex, "Protobuf-net consumer failed. RetryCount={RetryCount}", retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, ProtobufNetLoadMessage? message, Exception? ex)
    {
        _logger.LogError(ex, "Protobuf-net consumer fallback.");
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer(LoadTestRoutes.MessagePack, Qos = 1000)]
public sealed class MessagePackLoadConsumer : IConsumer<MessagePackLoadMessage>
{
    private readonly ILogger<MessagePackLoadConsumer> _logger;
    private static long _count;

    public MessagePackLoadConsumer(ILogger<MessagePackLoadConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, MessagePackLoadMessage message)
    {
        var current = Interlocked.Increment(ref _count);
        if (current % 1_000 == 0)
        {
            _logger.LogInformation("MessagePack consumer processed {Count} messages.", current);
        }

        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, MessagePackLoadMessage message)
    {
        _logger.LogWarning(ex, "MessagePack consumer failed. RetryCount={RetryCount}", retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, MessagePackLoadMessage? message, Exception? ex)
    {
        _logger.LogError(ex, "MessagePack consumer fallback.");
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer(LoadTestRoutes.RawBinary, Qos = 1000)]
public sealed class RawBinaryLoadConsumer : IConsumer<RawBinaryLoadMessage>
{
    private readonly ILogger<RawBinaryLoadConsumer> _logger;
    private static long _count;

    public RawBinaryLoadConsumer(ILogger<RawBinaryLoadConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, RawBinaryLoadMessage message)
    {
        var current = Interlocked.Increment(ref _count);
        if (current % 1_000 == 0)
        {
            _logger.LogInformation("Raw binary consumer processed {Count} messages.", current);
        }

        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, RawBinaryLoadMessage message)
    {
        _logger.LogWarning(ex, "Raw binary consumer failed. RetryCount={RetryCount}", retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, RawBinaryLoadMessage? message, Exception? ex)
    {
        _logger.LogError(ex, "Raw binary consumer fallback.");
        return Task.FromResult(ConsumerState.Ack);
    }
}
