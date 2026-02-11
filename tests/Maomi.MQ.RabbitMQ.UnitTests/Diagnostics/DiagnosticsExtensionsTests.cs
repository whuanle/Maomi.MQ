using System.Diagnostics;

namespace Maomi.MQ.RabbitMQ.UnitTests.Diagnostics;

public class DiagnosticsExtensionsTests
{
#if NET7_0_OR_GREATER && !NET9_0_OR_GREATER
    [Fact]
    public void AddException_WithNullException_ShouldThrow()
    {
        using var activity = new Activity("test").Start();

        Assert.Throws<ArgumentNullException>(() => activity.AddException(null!));
    }

    [Fact]
    public void AddException_ShouldAppendExceptionEvent()
    {
        using var activity = new Activity("test").Start();

        activity.AddException(new InvalidOperationException("boom"));

        Assert.Contains(activity.Events, x => x.Name == "exception");
    }
#else
    [Fact]
    public void Placeholder_ForNet9Plus()
    {
        Assert.True(true);
    }
#endif
}
