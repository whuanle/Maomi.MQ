using Maomi.MQ.Default;
using Maomi.MQ.Tests;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Xunit;

namespace Maomi.MQ.Tests;

public class DefaultIdFactoryTests
{
    [Fact]
    public void NextId_UseServiceProvider()
    {
        var services = MaomiHostHelper.BuildEmpty();

        var ioc = services.BuildServiceProvider();
        var idFactory = ioc.GetRequiredService<IIdFactory>();
        var id = idFactory.NextId();

        Assert.True(id > 0);
    }

    [Fact]
    public void NextId_ShouldReturnUniqueIds()
    {
        var factory = new DefaultIdFactory(1);

        var id1 = factory.NextId();
        var id2 = factory.NextId();

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void NextId_ShouldReturnPositiveId()
    {
        var factory = new DefaultIdFactory(1);

        var id = factory.NextId();

        Assert.True(id > 0);
    }

    [Fact]
    public async Task NextId_ShouldReturnUniqueIdsInParallel()
    {
        var factory = new DefaultIdFactory(1);
        var ids = new ConcurrentBag<long>();
        var tasks = Enumerable.Range(0, 10000).Select(_ => Task.Run(() => ids.Add(factory.NextId()))).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(10000, ids.Count);
        Assert.Equal(10000, ids.Distinct().Count());
    }

    [Fact]
    public async Task NextId_ShouldReturnUniqueIdsInParallel_DifferentWorkId()
    {
        var factory1 = new DefaultIdFactory(1);
        var factory2 = new DefaultIdFactory(2);

        var ids = new ConcurrentBag<long>();
        var tasks1 = Enumerable.Range(0, 10000).Select(_ => Task.Run(() => ids.Add(factory1.NextId()))).ToArray();
        var tasks2 = Enumerable.Range(0, 10000).Select(_ => Task.Run(() => ids.Add(factory2.NextId()))).ToArray();

        var tasksList = new List<Task>();
        tasksList.AddRange(tasks1);
        tasksList.AddRange(tasks2);

        await Task.WhenAll(tasksList);

        Assert.Equal(20000, ids.Count);
        Assert.Equal(20000, ids.Distinct().Count());
    }
}
