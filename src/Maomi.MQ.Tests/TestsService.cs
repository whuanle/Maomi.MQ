using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Maomi.MQ.Tests;

public class TestsService
{
    public IHost BuildHost()
    {
        var host = new HostBuilder()
            .ConfigureLogging(options =>
            {
                options.AddConsole();
                options.AddDebug();
            })
            .ConfigureHostConfiguration(options =>
            {
                options
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json");
            })
            .ConfigureServices((context, services) =>
            {
                var rabbitmqHostName = context.Configuration["RabbitMQ:HostName"]!;

                services.AddMaomiMQ(options =>
                {
                    options.WorkId = 1;
                }, options =>
                {
                    options.HostName = rabbitmqHostName;
                }, typeof(TestsService).Assembly);
            }).Build();

        return host;
    }
}
