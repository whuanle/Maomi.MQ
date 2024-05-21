using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Maomi.MQ.Tests;
public static class MaomiHostHelper
{
    public static IServiceCollection BuildEmpty()
    {
        var services = new ServiceCollection();
        services.AddMaomiMQ(mq =>
        {
        }, mq =>
        {
        }, Array.Empty<Assembly>());
        return services;
    }
}
