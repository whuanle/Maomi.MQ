using Maomi.MQ;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maomi.MQ.RabbitMQ.UnitTests.EventBus;

public class HandlerMediatorTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldInvokeHandlersInOrder()
    {
        var callOrder = new List<int>();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestMessage>>(new OrderedHandler(1, callOrder));
        services.AddSingleton<IEventHandler<TestMessage>>(new OrderedHandler(2, callOrder));
        services.AddSingleton(typeof(HandlerA), new HandlerA(callOrder));
        services.AddSingleton(typeof(HandlerB), new HandlerB(callOrder));

        var handlerMap = new Dictionary<int, Type>
        {
            [1] = typeof(HandlerA),
            [2] = typeof(HandlerB),
        };

        services.AddSingleton<IEventHandlerFactory<TestMessage>>(new EventHandlerFactory<TestMessage>(handlerMap));

        using var provider = services.BuildServiceProvider();
        var mediator = new HandlerMediator<TestMessage>(provider, provider.GetRequiredService<IEventHandlerFactory<TestMessage>>(), NullLoggerFactory.Instance);

        await mediator.ExecuteAsync(new MessageHeader(), new TestMessage(), CancellationToken.None);

        Assert.Equal([1, 2], callOrder);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandlerThrows_ShouldCancelExecutedHandlersInReverseOrder()
    {
        var callOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(typeof(HandlerC), new HandlerC(callOrder));
        services.AddSingleton(typeof(HandlerThrow), new HandlerThrow(callOrder));

        var handlerMap = new Dictionary<int, Type>
        {
            [1] = typeof(HandlerC),
            [2] = typeof(HandlerThrow),
        };

        services.AddSingleton<IEventHandlerFactory<TestMessage>>(new EventHandlerFactory<TestMessage>(handlerMap));

        using var provider = services.BuildServiceProvider();
        var mediator = new HandlerMediator<TestMessage>(provider, provider.GetRequiredService<IEventHandlerFactory<TestMessage>>(), NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.ExecuteAsync(new MessageHeader(), new TestMessage(), CancellationToken.None));

        Assert.Equal(["exec-1", "exec-2", "cancel-2", "cancel-1"], callOrder);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(HandlerA), new HandlerA(new List<int>()));

        var handlerMap = new Dictionary<int, Type>
        {
            [1] = typeof(HandlerA),
        };

        services.AddSingleton<IEventHandlerFactory<TestMessage>>(new EventHandlerFactory<TestMessage>(handlerMap));

        using var provider = services.BuildServiceProvider();
        var mediator = new HandlerMediator<TestMessage>(provider, provider.GetRequiredService<IEventHandlerFactory<TestMessage>>(), NullLoggerFactory.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => mediator.ExecuteAsync(new MessageHeader(), new TestMessage(), cts.Token));
    }

    private sealed class TestMessage
    {
    }

    private sealed class OrderedHandler : IEventHandler<TestMessage>
    {
        private readonly int _index;
        private readonly List<int> _calls;

        public OrderedHandler(int index, List<int> calls)
        {
            _index = index;
            _calls = calls;
        }

        public Task ExecuteAsync(TestMessage message, CancellationToken cancellationToken)
        {
            _calls.Add(_index);
            return Task.CompletedTask;
        }

        public Task CancelAsync(TestMessage message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class HandlerA : IEventHandler<TestMessage>
    {
        private readonly List<int> _calls;

        public HandlerA(List<int> calls)
        {
            _calls = calls;
        }

        public Task ExecuteAsync(TestMessage message, CancellationToken cancellationToken)
        {
            _calls.Add(1);
            return Task.CompletedTask;
        }

        public Task CancelAsync(TestMessage message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class HandlerB : IEventHandler<TestMessage>
    {
        private readonly List<int> _calls;

        public HandlerB(List<int> calls)
        {
            _calls = calls;
        }

        public Task ExecuteAsync(TestMessage message, CancellationToken cancellationToken)
        {
            _calls.Add(2);
            return Task.CompletedTask;
        }

        public Task CancelAsync(TestMessage message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class HandlerC : IEventHandler<TestMessage>
    {
        private readonly List<string> _calls;

        public HandlerC(List<string> calls)
        {
            _calls = calls;
        }

        public Task ExecuteAsync(TestMessage message, CancellationToken cancellationToken)
        {
            _calls.Add("exec-1");
            return Task.CompletedTask;
        }

        public Task CancelAsync(TestMessage message, CancellationToken cancellationToken)
        {
            _calls.Add("cancel-1");
            return Task.CompletedTask;
        }
    }

    private sealed class HandlerThrow : IEventHandler<TestMessage>
    {
        private readonly List<string> _calls;

        public HandlerThrow(List<string> calls)
        {
            _calls = calls;
        }

        public Task ExecuteAsync(TestMessage message, CancellationToken cancellationToken)
        {
            _calls.Add("exec-2");
            throw new InvalidOperationException("fail");
        }

        public Task CancelAsync(TestMessage message, CancellationToken cancellationToken)
        {
            _calls.Add("cancel-2");
            return Task.CompletedTask;
        }
    }
}
