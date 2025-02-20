using System.Diagnostics;

namespace Maomi.MQ.Tests;

public class DiagnosticsExtensionsTests
{

#if NET7_0_OR_GREATER && !NET9_0_OR_GREATER

    [Fact]
    public void AddException_ShouldAddExceptionDetailsToActivity()
    {
        var activity = new Activity("TestActivity");
        var exception = new InvalidOperationException("Test exception");

        activity.Start();
        activity.AddException(exception);
        activity.Stop();

        var exceptionEvent = Assert.Single(activity.Events);
        Assert.Equal("exception", exceptionEvent.Name);
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && ((string)tag.Value!) == "Test exception");
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.stacktrace" && tag.Value != null);
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && ((string)tag.Value!) == typeof(InvalidOperationException).ToString());
    }

    [Fact]
    public void AddException_ShouldThrowArgumentNullException_WhenExceptionIsNull()
    {
        var activity = new Activity("TestActivity");

        Assert.Throws<ArgumentNullException>(() => activity.AddException(null!));
    }

#endif

}
