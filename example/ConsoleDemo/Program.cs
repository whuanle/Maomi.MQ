using Maomi.MQ;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Reflection;

var host = new HostBuilder()
    .ConfigureLogging(options =>
    {
        options.AddConsole();
        options.AddDebug();
    })
    .ConfigureServices(services =>
    {
        services.AddMaomiMQ(options =>
        {
            options.WorkId = 1;
            options.AppName = "myapp";
            options.Rabbit = (ConnectionFactory options) =>
            {
                options.HostName = Environment.GetEnvironmentVariable("RABBITMQ")!;
                options.Port = 5672;
                options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
            };
        }, new System.Reflection.Assembly[] { typeof(Program).Assembly });

    }).Build();

// 后台运行
var task =  host.RunAsync();

Console.ReadLine();
