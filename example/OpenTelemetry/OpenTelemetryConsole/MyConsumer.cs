﻿using Maomi.MQ;

[Consumer("opentelemetry_console", Qos = 100, RetryFaildRequeue = true)]
public class MyConsumer : IConsumer<TestEvent>
{
    private static volatile int _retryCount = 0;


    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        var count = Interlocked.Increment(ref _retryCount);
        Console.WriteLine($"event id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message) => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}

[Consumer("opentelemetry_console2", Qos = 100, RetryFaildRequeue = true)]
public class MyConsumer2 : IConsumer<TestEvent>
{
    private static volatile int _retryCount = 0;

    // 消费
    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        var count = Interlocked.Increment(ref _retryCount);
        Console.WriteLine($"event id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }


    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message) => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}

[Consumer("opentelemetry_console3", Qos = 100, RetryFaildRequeue = true)]
public class MyConsumer3 : IConsumer<TestEvent>
{
    private static volatile int _retryCount = 0;

    // 消费
    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        var count = Interlocked.Increment(ref _retryCount);
        Console.WriteLine($"event id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message) => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}
