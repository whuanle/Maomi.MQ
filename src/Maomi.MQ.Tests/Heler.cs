using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Maomi.MQ.Diagnostics.DiagnosticName;
using static Maomi.MQ.Tests.Publish.MessagePublisherTests;

namespace Maomi.MQ.Tests;
public static class Heler
{
    public static ILogger<T> CreateLogger<T>()
    {
        return NullLogger<T>.Instance;
    }

    public static EventBody<TMessage> CreateEvent<TMessage>(long id, string queue, TMessage message)
        where TMessage : class
    {
        return new EventBody<TMessage>()
        {
            Id = id,
            Queue = queue,
            CreationTime = DateTimeOffset.Now,
            Body = message
        };
    }
}
