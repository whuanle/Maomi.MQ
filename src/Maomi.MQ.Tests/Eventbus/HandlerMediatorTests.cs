using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maomi.MQ.Tests.Eventbus;

public partial class HandlerMediatorTests
{
    [Fact]
    public async Task Handler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(UsableEvent));
        typeFilter.Filter(services, typeof(Usable_0_EventHandler<UsableEvent>));
        typeFilter.Filter(services, typeof(Usable_1_EventHandler<UsableEvent>));
        typeFilter.Filter(services, typeof(Usable_2_EventHandler<UsableEvent>));
        typeFilter.Filter(services, typeof(Usable_3_EventHandler<UsableEvent>));
        typeFilter.Filter(services, typeof(Usable_4_EventHandler<UsableEvent>));
        typeFilter.Filter(services, typeof(Usable_5_EventHandler<UsableEvent>));

        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();
        var h0 = ioc.GetRequiredService<Usable_0_EventHandler<UsableEvent>>();
        var h1 = ioc.GetRequiredService<Usable_1_EventHandler<UsableEvent>>();
        var h2 = ioc.GetRequiredService<Usable_2_EventHandler<UsableEvent>>();
        var h3 = ioc.GetRequiredService<Usable_3_EventHandler<UsableEvent>>();
        var h4 = ioc.GetRequiredService<Usable_4_EventHandler<UsableEvent>>();
        var h5 = ioc.GetRequiredService<Usable_5_EventHandler<UsableEvent>>();

        var handlerMediator = ioc.GetRequiredService<IHandlerMediator<UsableEvent>>();

        await handlerMediator.ExecuteAsync(Heler.CreateEvent(1, "test", new UsableEvent { Id = 1 }), CancellationToken.None);

        Assert.Equal(1, h0.HandlerCount);
        Assert.Equal(1, h1.HandlerCount);
        Assert.Equal(1, h2.HandlerCount);
        Assert.Equal(1, h3.HandlerCount);
        Assert.Equal(1, h4.HandlerCount);
        Assert.Equal(1, h5.HandlerCount);

        Assert.Equal(0, h0.CancelCount);
        Assert.Equal(0, h1.CancelCount);
        Assert.Equal(0, h2.CancelCount);
        Assert.Equal(0, h3.CancelCount);
        Assert.Equal(0, h4.CancelCount);
        Assert.Equal(0, h5.CancelCount);
    }

    [Fact]
    public async Task Handler_Exception()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(ExceptionEvent));
        typeFilter.Filter(services, typeof(Exception_0_EventHandler<ExceptionEvent>));
        typeFilter.Filter(services, typeof(Exception_1_EventHandler<ExceptionEvent>));
        typeFilter.Filter(services, typeof(Exception_2_EventHandler<ExceptionEvent>));
        typeFilter.Filter(services, typeof(Exception_3_EventHandler<ExceptionEvent>));
        typeFilter.Filter(services, typeof(Exception_4_EventHandler<ExceptionEvent>));
        typeFilter.Filter(services, typeof(Exception_5_EventHandler<ExceptionEvent>));

        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();
        var h0 = ioc.GetRequiredService<Exception_0_EventHandler<ExceptionEvent>>();
        var h1 = ioc.GetRequiredService<Exception_1_EventHandler<ExceptionEvent>>();
        var h2 = ioc.GetRequiredService<Exception_2_EventHandler<ExceptionEvent>>();
        var h3 = ioc.GetRequiredService<Exception_3_EventHandler<ExceptionEvent>>();
        var h4 = ioc.GetRequiredService<Exception_4_EventHandler<ExceptionEvent>>();
        var h5 = ioc.GetRequiredService<Exception_5_EventHandler<ExceptionEvent>>();

        var handlerMediator = ioc.GetRequiredService<IHandlerMediator<ExceptionEvent>>();


        try
        {
            await handlerMediator.ExecuteAsync(Heler.CreateEvent(1, "test", new ExceptionEvent { Id = 1 }), CancellationToken.None);
        }
        catch (Exception ex)
        {
            Assert.True(ex is OperationCanceledException);
        }

        Assert.Equal(1, h0.HandlerCount);
        Assert.Equal(1, h1.HandlerCount);
        Assert.Equal(1, h2.HandlerCount);
        Assert.Equal(1, h3.HandlerCount);
        Assert.Equal(1, h4.HandlerCount);
        Assert.Equal(0, h5.HandlerCount);

        Assert.Equal(1, h0.CancelCount);
        Assert.Equal(1, h1.CancelCount);
        Assert.Equal(1, h2.CancelCount);
        Assert.Equal(1, h3.CancelCount);
        Assert.Equal(1, h4.CancelCount);
        Assert.Equal(1, h5.CancelCount);

        Assert.Null(h5.HandlerTime);
        Assert.True(h4.HandlerTime > h3.HandlerTime);
        Assert.True(h3.HandlerTime > h2.HandlerTime);
        Assert.True(h2.HandlerTime > h1.HandlerTime);
        Assert.True(h1.HandlerTime > h0.HandlerTime);

        Assert.True(h0.CancelTime > h1.CancelTime);
        Assert.True(h1.CancelTime > h2.CancelTime);
        Assert.True(h2.CancelTime > h3.CancelTime);
        Assert.True(h3.CancelTime > h4.CancelTime);
        Assert.True(h4.CancelTime > h5.CancelTime);
    }
}
