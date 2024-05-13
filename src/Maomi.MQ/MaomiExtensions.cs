using IdGen;
using Maomi.MQ.Extensions;
using Maomi.MQ.Helpers;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System.Reflection;

namespace Maomi.MQ
{

    public static class MaomiExtensions
    {
        private static readonly IReadOnlyList<ITypeFilter> TypeFilters = new List<ITypeFilter>()
        {

        };

        public static IServiceCollection AddMaomiMQ(this IServiceCollection services, Action<ConnectionOptions> connectionAction, Action<ConnectionFactory> factoryAction, params Assembly[] assemblies)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory();

            if (factoryAction != null)
            {
                factoryAction.Invoke(connectionFactory);
            }

            var connectionOptions = new DefaultConnectionOptions
            {
                ConnectionFactory = connectionFactory
            };

            if (connectionAction != null)
            {
                connectionAction.Invoke(connectionOptions);
            }

            services.AddIdGen(connectionOptions.WorkId, () => IdGeneratorOptions.Default);

            services.AddSingleton<ConnectionOptions>(connectionOptions);
            services.AddSingleton(connectionOptions);
            services.AddSingleton<ConnectionPooledObjectPolicy>();
            services.AddSingleton<ConnectionPool>();

            services.AddSingleton<IPolicyFactory, DefaultPolicyFactory>();

            services.AddSingleton<IJsonSerializer, DefaultJsonSerializer>();
            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            List<Type> types = new();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var item in TypeFilters)
                    {
                        item.Filter(type, services);
                    }
                }
            }

            foreach (var item in TypeFilters)
            {
                item.Build(services);
            }
            return services;
        }

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
                    .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(ISingleConsumer<>));
                var multipleConsumerInterface = type.GetInterfaces().Where(x => x.IsGenericType)
                    .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IMultipleConsumer<>));

                if (singleConsumerInterface == null && multipleConsumerInterface == null)
                {
                    return;
                }

                Type? hostType = null;
                if (singleConsumerInterface != null)
                {
                    var eventType = singleConsumerInterface.GenericTypeArguments[0];
                    hostType = typeof(SingleHostSrvice<,>).MakeGenericType(type, eventType);
                    services.Add(new ServiceDescriptor(singleConsumerInterface, type, ServiceLifetime.Transient));
                }
                else if (multipleConsumerInterface != null)
                {
                    hostType = typeof(MultipleHostSrvice<,>).MakeGenericType(type, multipleConsumerInterface.GenericTypeArguments[0]);
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
