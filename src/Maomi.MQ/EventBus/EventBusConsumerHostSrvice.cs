using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ.EventBus
{
    public class EventBusConsumerHostSrvice<TConsumer, TEvent> : ConsumerBaseHostSrvice<TConsumer, TEvent>
        where TEvent : class
        where TConsumer : IConsumer<TEvent>
    {
        public EventBusConsumerHostSrvice(IServiceProvider serviceProvider,
            DefaultMqOptions connectionOptions,
            IJsonSerializer jsonSerializer,
            ILogger<ConsumerBaseHostSrvice<TConsumer, TEvent>> logger,
            IPolicyFactory policyFactory) :
            this(serviceProvider, connectionOptions, jsonSerializer, logger, policyFactory, serviceProvider.GetRequiredKeyedService<ConsumerOptions>(typeof(TEvent)))
        {
        }

        protected EventBusConsumerHostSrvice(IServiceProvider serviceProvider,
            DefaultMqOptions connectionOptions,
            IJsonSerializer jsonSerializer,
            ILogger<ConsumerBaseHostSrvice<TConsumer, TEvent>> logger,
            IPolicyFactory policyFactory, ConsumerOptions consumerOptions) :
            base(serviceProvider, connectionOptions, jsonSerializer, logger, policyFactory, consumerOptions)
        {
        }
    }
}
