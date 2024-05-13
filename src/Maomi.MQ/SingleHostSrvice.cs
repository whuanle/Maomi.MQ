using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;

namespace Maomi.MQ
{
    /// <summary>
    /// 串行服务，严格保证逐个消费
    /// </summary>
    public class SingleHostSrvice<TConsumer, TEvent> : BackgroundService
        where TEvent : class
        where TConsumer : ISingleConsumer<TEvent>
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly DefaultConnectionOptions _connectionOptions;

        protected readonly ConnectionFactory _connectionFactory;
        protected readonly Type _consumerType;
        protected readonly ConsumerAttribute _consumerAttribute;
        protected readonly string _queueName;
        protected readonly IJsonSerializer _jsonSerializer;
        private readonly IPolicyFactory _policyFactory;

        protected readonly ILogger<SingleHostSrvice<TConsumer, TEvent>> _logger;

        public SingleHostSrvice(IServiceProvider serviceProvider, DefaultConnectionOptions connectionOptions, IJsonSerializer jsonSerializer, ILogger<MQ.SingleHostSrvice<TConsumer, TEvent>> logger, IPolicyFactory policyFactory)
        {
            _jsonSerializer = jsonSerializer;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _consumerType = typeof(TConsumer);
            _connectionOptions = connectionOptions;
            _connectionFactory = connectionOptions.ConnectionFactory;

            var consumerAttribute = _consumerType.GetCustomAttribute<ConsumerAttribute>();
            if (consumerAttribute == null)
            {
                consumerAttribute = new ConsumerAttribute();
            }
            _consumerAttribute = consumerAttribute;

            var eventQueue = typeof(TEvent).GetCustomAttribute<EventTopicAttribute>();
            if (eventQueue == null)
            {
                throw new InvalidOperationException($"{typeof(TEvent).Name} 没有配置 [EventQueue] 特性");
            }

            _queueName = connectionOptions.QueuePrefix + eventQueue.Queue;
            _policyFactory = policyFactory;
        }


        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using (IConnection connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (IChannel channel = await connection.CreateChannelAsync())
                {
                    // 根据消费者创建队列
                    await channel.QueueDeclareAsync(
                        queue: _queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        // todo: 后期优化
                        arguments: null
                        );
                }
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scope = _serviceProvider.CreateScope();
            var ioc = scope.ServiceProvider;

            using IConnection connection = await _connectionFactory.CreateConnectionAsync();
            using IChannel channel = await connection.CreateChannelAsync();
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            // 定义消费者
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (sender, eventArgs) =>
            {
                await ConsumerAsync(channel, sender, eventArgs);

            };

            await channel.BasicConsumeAsync(queue: _queueName,
                                 autoAck: false,
                                 consumer: consumer);
            while (true)
            {
                await Task.Delay(1000);
            }
        }

        protected virtual async Task ConsumerAsync(IChannel channel, object? sender, BasicDeliverEventArgs eventArgs)
        {
            var scope = _serviceProvider.CreateScope();
            var ioc = scope.ServiceProvider;

            var consumer = ioc.GetRequiredService<ISingleConsumer<TEvent>>();
       
            try
            {
                var eventBody = _jsonSerializer.Deserialize<EventBody<TEvent>>(eventArgs.Body.Span)!;

                var retryPolicy = _policyFactory.CreatePolicy(_queueName);

                // 创建异步回退策略
                var fallbackPolicy = Policy
                    .Handle<Exception>()
                    .FallbackAsync(async (c) =>
                    {
                        await consumer.FallbackAsync(eventBody);
                    });

                var policyWrap = Policy.WrapAsync(fallbackPolicy, retryPolicy);

                await policyWrap.ExecuteAsync(async () =>
                {
                    await consumer.ExecuteAsync(eventBody);
                });
                await channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: true);
                throw;
            }
        }
    }

    /// <summary>
    /// 并发消费服务
    /// </summary>
    public class MultipleHostSrvice<TConsumer, TEvent> : BackgroundService
        where TEvent : class
        where TConsumer : IMultipleConsumer<TEvent>
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly DefaultConnectionOptions _connectionOptions;

        protected readonly ConnectionFactory _connectionFactory;
        protected readonly Type _consumerType;
        protected readonly ConsumerAttribute _consumerAttribute;
        protected readonly string _queueName;
        protected readonly IJsonSerializer _jsonSerializer;
        private readonly IPolicyFactory _policyFactory;

        protected readonly ILogger<MultipleHostSrvice<TConsumer, TEvent>> _logger;

        public MultipleHostSrvice(IServiceProvider serviceProvider, DefaultConnectionOptions connectionOptions, IJsonSerializer jsonSerializer, ILogger<MQ.MultipleHostSrvice<TConsumer, TEvent>> logger, IPolicyFactory policyFactory)
        {
            _jsonSerializer = jsonSerializer;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _consumerType = typeof(TConsumer);
            _connectionOptions = connectionOptions;
            _connectionFactory = connectionOptions.ConnectionFactory;

            var consumerAttribute = _consumerType.GetCustomAttribute<ConsumerAttribute>();
            if (consumerAttribute == null)
            {
                consumerAttribute = new ConsumerAttribute();
            }
            _consumerAttribute = consumerAttribute;


            var eventQueue = typeof(TEvent).GetCustomAttribute<EventTopicAttribute>();
            if (eventQueue == null)
            {
                throw new InvalidOperationException($"{typeof(TEvent).Name} 没有配置 [EventQueue] 特性");
            }

            _queueName = connectionOptions.QueuePrefix + eventQueue.Queue;
            _policyFactory = policyFactory;
        }


        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using (IConnection connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (IChannel channel = await connection.CreateChannelAsync())
                {
                    // 根据消费者创建队列
                    await channel.QueueDeclareAsync(
                        queue: _queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        // todo: 后期优化
                        arguments: null
                        );
                }
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scope = _serviceProvider.CreateScope();
            var ioc = scope.ServiceProvider;

            using IConnection connection = await _connectionFactory.CreateConnectionAsync();
            using IChannel channel = await connection.CreateChannelAsync();
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _consumerAttribute.Qos, global: false);

            // 定义消费者
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (sender, eventArgs) =>
            {
                await ConsumerAsync(channel, sender, eventArgs);

            };

            await channel.BasicConsumeAsync(queue: _queueName,
                                 autoAck: false,
                                 consumer: consumer);
            while (true)
            {
                await Task.Delay(1000);
            }
        }

        protected virtual async Task ConsumerAsync(IChannel channel, object? sender, BasicDeliverEventArgs eventArgs)
        {
            var scope = _serviceProvider.CreateScope();
            var ioc = scope.ServiceProvider;

            var consumer = ioc.GetRequiredService<ISingleConsumer<TEvent>>();

            try
            {
                var eventBody = _jsonSerializer.Deserialize<EventBody<TEvent>>(eventArgs.Body.Span)!;

                var retryPolicy = _policyFactory.CreatePolicy(_queueName);

                // 创建异步回退策略
                var fallbackPolicy = Policy
                    .Handle<Exception>()
                    .FallbackAsync(async (c) =>
                    {
                        await consumer.FallbackAsync(eventBody);
                    });

                var policyWrap = Policy.WrapAsync(fallbackPolicy, retryPolicy);

                await policyWrap.ExecuteAsync(async () =>
                {
                    await consumer.ExecuteAsync(eventBody);
                });
                await channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: true);
                throw;
            }
        }
    }
}
