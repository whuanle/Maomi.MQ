using Maomi.MQ;
using Maomi.MQ.Default;
using Maomi.MQ.Diagnostics;
using Moq;
using RabbitMQ.Client;

namespace Maomi.MQ.RabbitMQ.UnitTests.Default;

public class ServiceFactoryTests
{
    [Fact]
    public void Constructor_ShouldStoreAllDependencies()
    {
        var serviceProvider = new Mock<IServiceProvider>().Object;
        var options = new MqOptions
        {
            AppName = "app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = new Mock<IConnectionFactory>().Object,
            MessageSerializers = Array.Empty<IMessageSerializer>(),
        };

        var retry = new Mock<IRetryPolicyFactory>().Object;
        var ids = new Mock<IIdProvider>().Object;
        var diagnostics = new Mock<IConsumerDiagnostics>().Object;

        var factory = new ServiceFactory(serviceProvider, options, retry, ids, diagnostics);

        Assert.Same(serviceProvider, factory.ServiceProvider);
        Assert.Same(options, factory.Options);
        Assert.Same(retry, factory.RetryPolicyFactory);
        Assert.Same(ids, factory.Ids);
        Assert.Same(diagnostics, factory.ConsumerDiagnostics);
    }
}
