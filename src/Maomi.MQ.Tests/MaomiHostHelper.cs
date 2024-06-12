using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Maomi.MQ.Tests;
public static class MaomiHostHelper
{
    public static IServiceCollection BuildEmpty()
    {
        var services = new ServiceCollection();
        services.AddMaomiMQ(mq =>
        {
            mq.Rabbit = (o) => o.HostName = "127.0.0.1";
        }, Array.Empty<Assembly>());
        return services;
    }
}
