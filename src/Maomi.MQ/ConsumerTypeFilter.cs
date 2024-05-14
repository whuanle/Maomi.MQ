using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Maomi.MQ
{

    public static partial class MaomiExtensions
    {
        // 1，事件总线的支持
        // 2，barrier 原子性支持
        // 3，重试策略持久化
        // 4，消息附加属性和雪花id

        public class ConsumerTypeFilter : ITypeFilter
        {
            private static readonly MethodInfo AddHostedMethod = typeof(ServiceCollectionHostedServiceExtensions)
                .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), BindingFlags.Static | BindingFlags.Public, [typeof(IServiceCollection)])!;

            public ConsumerTypeFilter()
            {
                ArgumentNullException.ThrowIfNull(AddHostedMethod);
            }

            public void Build(IServiceCollection services)
            {
            }

            public void Filter(Type type, IServiceCollection services)
            {
                if (!type.IsClass)
                {
                    return;
                }

                var singleConsumerInterface = type.GetInterfaces().Where(x => x.IsGenericType)
                    .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>));
                var multipleConsumerInterface = type.GetInterfaces().Where(x => x.IsGenericType)
                    .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>));

                if (singleConsumerInterface == null && multipleConsumerInterface == null)
                {
                    return;
                }

                Type? hostType = null;
                if (singleConsumerInterface != null)
                {
                    var eventType = singleConsumerInterface.GenericTypeArguments[0];
                    hostType = typeof(ConsumerHostSrvice<,>).MakeGenericType(type, eventType);
                    services.Add(new ServiceDescriptor(singleConsumerInterface, type, ServiceLifetime.Transient));
                }
                else if (multipleConsumerInterface != null)
                {
                    hostType = typeof(ConsumerHostSrvice<,>).MakeGenericType(type, multipleConsumerInterface.GenericTypeArguments[0]);
                    services.Add(new ServiceDescriptor(multipleConsumerInterface, type, ServiceLifetime.Transient));
                }

                if (hostType != null)
                {
                    AddHostedMethod.MakeGenericMethod(hostType).Invoke(null, new object[] { services });
                }
            }
        }
    }
}
