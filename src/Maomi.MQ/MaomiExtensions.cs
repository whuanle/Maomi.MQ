using IdGen;
using Maomi.MQ.Defaults;
using Maomi.MQ.EventBus;
using Maomi.MQ.Extensions;
using Maomi.MQ.Pool;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Reflection;

namespace Maomi.MQ
{

    public static partial class MaomiExtensions
    {
        public static IServiceCollection AddMaomiMQ(this IServiceCollection services, 
            Action<ConnectionOptions> connectionAction,
            Action<ConnectionFactory> factoryAction, 
            params Assembly[] assemblies)
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
            var typeFilters = new List<ITypeFilter>()
            {
                new ConsumerTypeFilter(),
                new EventBusTypeFilter()
            };
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var item in typeFilters)
                    {
                        item.Filter(type, services);
                    }
                }
            }

            foreach (var item in typeFilters)
            {
                item.Build(services);
            }
            return services;
        }
    }
}
