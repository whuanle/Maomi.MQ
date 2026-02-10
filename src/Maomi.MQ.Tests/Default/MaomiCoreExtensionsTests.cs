using Maomi.MQ;
using Maomi.MQ.Default;
using Maomi.MQ.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maomi.MQ.Tests;

public class MaomiCoreExtensionsTests
{
    [Fact]
    public void AddMaomiMQCore_ShouldRegisterServices()
    {
        var services = new ServiceCollection();

        services.AddMaomiMQCore();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<IMessageSerializer>());
        Assert.IsType<DefaultJsonMessageSerializer>(serviceProvider.GetService<IMessageSerializer>());

        Assert.NotNull(serviceProvider.GetService<IRetryPolicyFactory>());
        Assert.IsType<DefaultRetryPolicyFactory>(serviceProvider.GetService<IRetryPolicyFactory>());

        Assert.NotNull(serviceProvider.GetService<IIdProvider>());
        Assert.IsType<DefaultIdProvider>(serviceProvider.GetService<IIdProvider>());
    }
}
