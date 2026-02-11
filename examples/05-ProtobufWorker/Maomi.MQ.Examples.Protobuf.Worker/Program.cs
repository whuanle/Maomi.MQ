using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProtoBuf;
using RabbitMQ.Client;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        var protobufSerializer = new ProtobufMessageSerializer();
        options.WorkId = 5;
        options.AppName = "protobuf-worker";
        options.MessageSerializers = serializers =>
        {
            serializers.Insert(0, protobufSerializer);
        };
        options.Rabbit = rabbit =>
        {
            rabbit.HostName = builder.Configuration["RabbitMQ:Host"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ")
                ?? "127.0.0.1";
            rabbit.Port = builder.Configuration.GetValue<int?>("RabbitMQ:Port") ?? 5672;
            rabbit.UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            rabbit.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
            rabbit.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
        };
    },
    [typeof(Program).Assembly]);

builder.Services.AddHostedService<PublisherWorker>();

var host = builder.Build();
await host.RunAsync();

[ProtoContract]
[QueueName("example.protobuf.person")]
public sealed class PersonMessage
{
    [ProtoMember(1)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    [ProtoMember(3)]
    public int Age { get; set; }
}

[Consumer("example.protobuf.person", Qos = 5)]
public sealed class PersonMessageConsumer : IConsumer<PersonMessage>
{
    private readonly ILogger<PersonMessageConsumer> _logger;

    public PersonMessageConsumer(ILogger<PersonMessageConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, PersonMessage message)
    {
        _logger.LogInformation(
            "Protobuf message consumed. HeaderId={HeaderId}, PersonId={PersonId}, Name={Name}, Age={Age}",
            messageHeader.Id,
            message.Id,
            message.Name,
            message.Age);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, PersonMessage message)
    {
        _logger.LogWarning(
            ex,
            "Protobuf consume failed. HeaderId={HeaderId}, RetryCount={RetryCount}",
            messageHeader.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, PersonMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Protobuf fallback. HeaderId={HeaderId}, PersonId={PersonId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}

public sealed class PublisherWorker : BackgroundService
{
    private readonly ILogger<PublisherWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PublisherWorker(ILogger<PublisherWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        using var scope = _serviceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var message = new PersonMessage
        {
            Name = "protobuf-demo",
            Age = Random.Shared.Next(18, 50)
        };

        await publisher.AutoPublishAsync(message, cancellationToken: stoppingToken);
        _logger.LogInformation("Protobuf message published. PersonId={PersonId}", message.Id);
    }
}
