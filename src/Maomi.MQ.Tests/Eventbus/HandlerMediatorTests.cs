using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maomi.MQ.Tests.Eventbus;
public partial class HandlerMediatorTests
{
    [Fact]
    public async Task Handler_Exception()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(Group_Test1Event));
        typeFilter.Filter(services, typeof(TEventEventHandler_0<Group_Test1Event>));
        typeFilter.Filter(services, typeof(TEventEventHandler_1<Group_Test1Event>));
        typeFilter.Filter(services, typeof(TEventEventHandler_2<Group_Test1Event>));
        typeFilter.Filter(services, typeof(TEventEventHandler_3<Group_Test1Event>));

        typeFilter.Build(services);

        // Change life cycle.
        services.AddScoped<TEventEventHandler_0<Group_Test1Event>>();
        services.AddScoped<TEventEventHandler_1<Group_Test1Event>>();
        services.AddSingleton<TEventEventHandler_2<Group_Test1Event>>();
        services.AddScoped<TEventEventHandler_3<Group_Test1Event>>();

        var ioc = services.BuildServiceProvider();
        var h0 = ioc.GetRequiredService<TEventEventHandler_0<Group_Test1Event>>();
        var h1 = ioc.GetRequiredService<TEventEventHandler_1<Group_Test1Event>>();
        var h2 = ioc.GetRequiredService<TEventEventHandler_2<Group_Test1Event>>();
        var h3 = ioc.GetRequiredService<TEventEventHandler_3<Group_Test1Event>>();

        var handlerMediator = ioc.GetRequiredService<IHandlerMediator<Group_Test1Event>>();

        try
        {
            await handlerMediator.ExecuteAsync(new EventBody<Group_Test1Event>
            {
                Id = 1,
                Queue = "test1",
                CreationTime = DateTimeOffset.Now,
                Body = new Group_Test1Event
                {
                    Id = 1
                }
            },
            CancellationToken.None);
        }
        catch (Exception ex)
        {
            Assert.True(ex is OperationCanceledException);
        }

        Assert.True(h0.Handler);
        Assert.True(h1.Handler);
        Assert.True(h2.Handler);
        Assert.True(h0.Cancel);
        Assert.True(h1.Cancel);
        Assert.True(h2.Cancel);

        ioc = services.BuildServiceProvider();
        handlerMediator = ioc.GetRequiredService<IHandlerMediator<Group_Test1Event>>();
        var eventMiddleware = ioc.GetRequiredService<IEventMiddleware<Group_Test1Event>>();
        h0 = ioc.GetRequiredService<TEventEventHandler_0<Group_Test1Event>>();
        h1 = ioc.GetRequiredService<TEventEventHandler_1<Group_Test1Event>>();
        h2 = ioc.GetRequiredService<TEventEventHandler_2<Group_Test1Event>>();
        h3 = ioc.GetRequiredService<TEventEventHandler_3<Group_Test1Event>>();

        Assert.False(h0.Handler);
        Assert.False(h1.Handler);
        Assert.False(h2.Handler);
        Assert.False(h0.Cancel);
        Assert.False(h1.Cancel);
        Assert.False(h2.Cancel);

        try
        {
            await eventMiddleware.ExecuteAsync(new EventBody<Group_Test1Event>
            {
                Id = 1,
                Queue = "test1",
                CreationTime = DateTimeOffset.Now,
                Body = new Group_Test1Event
                {
                    Id = 1
                }
            },
            handlerMediator.ExecuteAsync);
        }
        catch (Exception ex)
        {
            Assert.True(ex is OperationCanceledException);
        }

        Assert.True(h0.Handler);
        Assert.True(h1.Handler);
        Assert.True(h2.Handler);
        Assert.True(h0.Cancel);
        Assert.True(h1.Cancel);
        Assert.True(h2.Cancel);
    }

    [EventTopic("test1", Group = "group")]
    public class Group_Test1Event
    {
        public int Id { get; set; }
    }

    public interface IHandlerRecord
    {
        public bool Handler { get; }
        public int HandlerCount { get; }
        public bool Cancel { get; }
        public int CancelCount { get; }
    }

    [EventOrder(0)]
    public class TEventEventHandler_0<TEvent> : IEventHandler<TEvent>, IHandlerRecord
    {
        public bool Handler { get; private set; }
        public bool Cancel { get; private set; }

        public int HandlerCount { get; private set; }

        public int CancelCount { get; private set; }

        public Task CancelAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            Cancel = true;
            CancelCount++;
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            Handler = true;
            HandlerCount++;
            return Task.CompletedTask;
        }
    }

    [EventOrder(1)]
    public class TEventEventHandler_1<TEvent> : IEventHandler<TEvent>, IHandlerRecord
    {
        public bool Handler { get; private set; }
        public bool Cancel { get; private set; }

        public int HandlerCount { get; private set; }

        public int CancelCount { get; private set; }

        public Task CancelAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            Cancel = true;
            CancelCount++;
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            Handler = true;
            HandlerCount++;
            return Task.CompletedTask;
        }
    }


    [EventOrder(2)]
    public class TEventEventHandler_2<TEvent> : IEventHandler<TEvent>, IHandlerRecord
    {
        public bool Handler { get; private set; }
        public bool Cancel { get; private set; }

        public int HandlerCount { get; private set; }

        public int CancelCount { get; private set; }

        public Task CancelAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            Cancel = true;
            CancelCount++;
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            Handler = true;
            HandlerCount++;
            return Task.CompletedTask;
        }
    }


    [EventOrder(3)]
    public class TEventEventHandler_3<TEvent> : IEventHandler<TEvent>
    {
        public Task CancelAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}
