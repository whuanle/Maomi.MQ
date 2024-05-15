using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// 消费者类型过滤器.
/// </summary>
public class ConsumerTypeFilter : ITypeFilter
{
    private static readonly MethodInfo AddHostedMethod = typeof(ServiceCollectionHostedServiceExtensions)
        .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), BindingFlags.Static | BindingFlags.Public, [typeof(IServiceCollection)])!;

    public ConsumerTypeFilter()
    {
        ArgumentNullException.ThrowIfNull(AddHostedMethod);
    }

    /// <inheritdoc/>
    public void Build(IServiceCollection services)
    {
    }

    public void Filter(Type type, IServiceCollection services)
    {
        if (!type.IsClass)
        {
            return;
        }

        var consumerInterface = type.GetInterfaces().Where(x => x.IsGenericType)
            .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>));

        if (consumerInterface == null)
        {
            return;
        }

        var consumerAttribute = type.GetCustomAttribute<ConsumerAttribute>();
        if (consumerAttribute == null || string.IsNullOrEmpty(consumerAttribute.Queue))
        {
            throw new ArgumentNullException($"{type.Name} type is not configured with the [Consumer] attribute.");
        }

        // 每个 IConsumer<T> 对应一个队列、一个 ConsumerHostSrvice<T>.
        services.Add(new ServiceDescriptor(consumerInterface, type, ServiceLifetime.Transient));

        var eventType = consumerInterface.GenericTypeArguments[0];
        var hostType = typeof(DefaultConsumerHostSrvice<,>).MakeGenericType(type, eventType);
        AddHostedMethod.MakeGenericMethod(hostType).Invoke(null, new object[] { services });
    }
}
