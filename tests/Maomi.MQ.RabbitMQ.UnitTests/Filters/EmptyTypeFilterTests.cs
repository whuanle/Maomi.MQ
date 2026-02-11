using Maomi.MQ.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.RabbitMQ.UnitTests.Filters;

public class EmptyTypeFilterTests
{
    [Fact]
    public void Build_ShouldAlwaysReturnEmpty()
    {
        var filter = new EmptyTypeFilter();
        var services = new ServiceCollection();

        Assert.Empty(filter.Build(services));
    }

    [Fact]
    public void Filter_ShouldNotChangeServices()
    {
        var filter = new EmptyTypeFilter();
        var services = new ServiceCollection();

        filter.Filter(services, typeof(string));

        Assert.Empty(services);
    }
}
