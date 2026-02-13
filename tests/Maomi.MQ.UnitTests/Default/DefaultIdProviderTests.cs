using Maomi.MQ.Default;

namespace Maomi.MQ.UnitTests.Default;

public class DefaultIdProviderTests
{
    [Fact]
    public void NextId_ShouldReturnPositiveValue()
    {
        var provider = new DefaultIdProvider(1);
        var id = provider.NextId();

        Assert.True(id > 0);
    }

    [Fact]
    public void NextId_ShouldReturnDifferentValuesAcrossCalls()
    {
        var provider = new DefaultIdProvider(1);

        var id1 = provider.NextId();
        var id2 = provider.NextId();

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public async Task NextId_ShouldBeUniqueInParallel()
    {
        var provider = new DefaultIdProvider(1);
        var ids = new long[4000];

        await Task.WhenAll(Enumerable.Range(0, ids.Length).Select(i => Task.Run(() => ids[i] = provider.NextId())));

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }
}
