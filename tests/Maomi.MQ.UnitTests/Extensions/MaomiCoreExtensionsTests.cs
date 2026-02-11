using Maomi.MQ;
using Maomi.MQ.Default;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.UnitTests.Extensions;

public class MaomiCoreExtensionsTests
{
    [Fact]
    public void AddMaomiMQCore_WithWorkId_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMaomiMQCore(5);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<DefaultRetryPolicyFactory>(provider.GetRequiredService<IRetryPolicyFactory>());
        Assert.IsType<DefaultIdProvider>(provider.GetRequiredService<IIdProvider>());
    }

    [Fact]
    public void AddMaomiMQCore_WithNullWorkId_ShouldStillRegisterIdProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMaomiMQCore(null);

        using var provider = services.BuildServiceProvider();
        var idProvider = provider.GetRequiredService<IIdProvider>();

        Assert.True(idProvider.NextId() > 0);
    }
}
