// <copyright file="CustomConsumerTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Maomi.MQ.Filters;

/// <summary>
/// Dynamic consumer type filter.<br />
/// 消费者类型过滤器.
/// </summary>
public class CustomConsumerTypeFilter : ITypeFilter
{
    private static readonly MethodInfo AddHostedMethod = typeof(ServiceCollectionHostedServiceExtensions)
        .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), BindingFlags.Static | BindingFlags.Public, [typeof(IServiceCollection)])!;

    private readonly List<(Type, IConsumerOptions)> _consumers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomConsumerTypeFilter"/> class.
    /// </summary>
    public CustomConsumerTypeFilter()
    {
        ArgumentNullException.ThrowIfNull(AddHostedMethod);
    }

    /// <inheritdoc/>
    public IEnumerable<ConsumerType> Build(IServiceCollection services)
    {
        List<ConsumerType> consumerTypes = new();
        foreach (var item in _consumers)
        {
            var (type, consumerOptions) = item;

            var queueName = consumerOptions.Queue;

            var consumerInterface = type.GetInterfaces().Where(x => x.IsGenericType)
                .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>))!;

            // Each IConsumer<T> corresponds to one queue and one ConsumerHostSrvice<T>.
            // 每个 IConsumer<T> 对应一个队列、一个 ConsumerHostSrvice<T>.
            services.Add(new ServiceDescriptor(serviceType: consumerInterface, implementationType: type, lifetime: ServiceLifetime.Scoped));

            var eventType = consumerInterface.GenericTypeArguments[0];
            var consumerType = new ConsumerType
            {
                Queue = queueName,
                Consumer = type,
                Event = eventType,
                ConsumerOptions = consumerOptions
            };

            consumerTypes.Add(consumerType);
        }

        return consumerTypes;
    }

    /// <summary>
    /// Add a custom consumer type.<br />
    /// 添加自定义消费者.
    /// </summary>
    /// <typeparam name="TConsumer"><see cref="IConsumer{TMessage}"/>.</typeparam>
    /// <param name="consumerOptions"></param>
    public void AddConsumer<TConsumer>(IConsumerOptions consumerOptions)
        where TConsumer : class
    {
        AddConsumer(typeof(TConsumer), consumerOptions);
    }

    /// <summary>
    /// Add a custom consumer type.<br />
    /// 添加自定义消费者.
    /// </summary>
    /// <param name="consumerType"></param>
    /// <param name="consumerOptions"></param>
    public void AddConsumer(Type consumerType, IConsumerOptions consumerOptions)
    {
        var consumerInterface = consumerType.GetInterfaces().Where(x => x.IsGenericType)
            .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>));

        if (consumerInterface == null)
        {
            throw new NotImplementedException($"{consumerType.Name} is not a valid consumer type");
        }

        _consumers.Add((consumerType, consumerOptions));
    }

    /// <inheritdoc/>
    public void Filter(IServiceCollection services, Type type)
    {
        return;
    }
}
