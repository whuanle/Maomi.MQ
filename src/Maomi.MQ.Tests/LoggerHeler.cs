using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maomi.MQ.Tests;
public static class LoggerHeler
{
    public static ILogger<T> Create<T>()
    {
        return NullLogger<T>.Instance;
    }
}
