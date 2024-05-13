using Maomi.MQ;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static ConsoleApp1.Program;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
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
                        //options.QueuePrefix = "a.";
                        options.WorkId = 1;
                    }, options =>
                    {
                        options.HostName = "192.168.3.248";
                    }, typeof(Program).Assembly);
                    services.AddHostedService<MyPublishAsync>();
                }).Build();

            await host.RunAsync();

        }

        public class MyPublishAsync : BackgroundService
        {
            private readonly IMessagePublisher _messagePublisher;

            public MyPublishAsync(IMessagePublisher messagePublisher)
            {
                _messagePublisher = messagePublisher;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                var publisher = _messagePublisher;

                for (int i = 0; i < 100; i++)
                {
                    await publisher.PublishAsync("aaaa", new TestEvent
                    {
                        Message = i.ToString()
                    });
                }

                while (true)
                {
                    await Task.Delay(1000);
                }
            }
        }

        
        [Consumer("aaaa",Qos = "")]
        public class MyConsumer : ISingleConsumer<TestEvent>
        {
            public async Task ExecuteAsync(EventBody<TestEvent> message)
            {
                Console.WriteLine(message.Body.Message);
                throw new NotImplementedException();
            }

            public async Task FaildAsync(EventBody<TestEvent> message)
            {
                await Task.CompletedTask;
            }

            public async Task FallbackAsync(EventBody<TestEvent> message)
            {

            }
        }

        [EventTopic("aaaaa",Qos = 10,Group = "b")]
        public class TestEvent
        {
            public string Message { get; set; }

            public override string ToString()
            {
                return Message;
            }
        }

        public class MyEventMiddleware : IEventMiddleware<TestEvent>
        {
            public async Task HandleAsync(EventBody<TestEvent> @event, EventHandlerDelegate<TestEvent> next)
            {
                await next(@event, CancellationToken.None);
            }
        }

        [EventOrder(0)]
        public class MyEvent : IEventHandler<TestEvent>
        {
            public async Task CancelAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
            {
            }

            public async Task HandlerAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
            {
            }
        }
    }
}
