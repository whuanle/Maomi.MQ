using Maomi.MQ.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Maomi.MQ.Tests.TypeFilter.EventbusGroupTypeFilterTests;

namespace Maomi.MQ.Tests.TypeFilter;
public partial class EventbusTypeFilterTests
{

    private class NoneTopicEvent
    {
        public int Id { get; set; }
    }

    private class NontTopicEventHandler : IEventHandler<NoneTopicEvent>
    {
        public Task CancelAsync(EventBody<NoneTopicEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteAsync(EventBody<NoneTopicEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventTopic("test")]
    private class NoneOrderEvent
    {
        public int Id { get; set; }
    }

    private class NoneOrderEventHandler : IEventHandler<NoneOrderEvent>
    {
        public Task CancelAsync(EventBody<NoneOrderEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteAsync(EventBody<NoneOrderEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventTopic("test")]
    private class EqualOrderEvent
    {
        public int Id { get; set; }
    }

    [EventOrder(0)]
    private class EqualOrder_1_EventHandler : IEventHandler<EqualOrderEvent>
    {
        public Task CancelAsync(EventBody<EqualOrderEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteAsync(EventBody<EqualOrderEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventOrder(0)]
    private class EqualOrder_2_EventHandler : IEventHandler<EqualOrderEvent>
    {
        public Task CancelAsync(EventBody<EqualOrderEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteAsync(EventBody<EqualOrderEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventTopic("test")]
    private class UsableEvent
    {
        public int Id { get; set; }
    }

    [EventOrder(0)]
    private class Usable_1_EventHandler : IEventHandler<UsableEvent>
    {
        public Task CancelAsync(EventBody<UsableEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteAsync(EventBody<UsableEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventOrder(1)]
    private class Usable_2_EventHandler : IEventHandler<UsableEvent>
    {
        public Task CancelAsync(EventBody<UsableEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteAsync(EventBody<UsableEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventTopic("test")]
    private class MiddlewareEvent
    {
        public int Id { get; set; }
    }

    private class TestEventMiddleware : IEventMiddleware<MiddlewareEvent>
    {
        public Task ExecuteAsync(EventBody<MiddlewareEvent> eventBody, EventHandlerDelegate<MiddlewareEvent> next)
        {
            return next(eventBody, CancellationToken.None);
        }

        public Task FaildAsync(Exception ex, int retryCount, EventBody<MiddlewareEvent>? message)
        {
            return Task.CompletedTask;
        }

        public Task<bool> FallbackAsync(EventBody<MiddlewareEvent>? message)
        {
            return Task.FromResult(true);
        }
    }

    [EventOrder(0)]
    private class MiddlewareEventHandler : IEventHandler<MiddlewareEvent>
    {
        public Task CancelAsync(EventBody<MiddlewareEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteAsync(EventBody<MiddlewareEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
