using Maomi.MQ.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maomi.MQ.Tests.Eventbus;
public partial class HandlerMediatorTests
{

    public interface IHandlerRecord
    {
        public DateTime? HandlerTime { get; }
        public DateTime? CancelTime { get; }
        public int HandlerCount { get; }
        public int CancelCount { get; }
    }

    public abstract class TMessageEventHandler<TMessage> : IEventHandler<TMessage>, IHandlerRecord
    {
        public DateTime? HandlerTime { get; private set; }
        public DateTime? CancelTime { get; private set; }

        public int HandlerCount { get; private set; }

        public int CancelCount { get; private set; }

        public virtual Task CancelAsync(EventBody<TMessage> message, CancellationToken cancellationToken)
        {
            CancelTime = DateTime.Now;
            CancelCount++;
            return Task.CompletedTask;
        }

        public virtual Task ExecuteAsync(EventBody<TMessage> message, CancellationToken cancellationToken)
        {
            HandlerTime = DateTime.Now;
            HandlerCount++;
            return Task.CompletedTask;
        }
    }

    [EventTopic("test")]
    public class UsableEvent
    {
        public int Id { get; set; }
    }

    [EventOrder(0)]
    public class Usable_0_EventHandler<UsableEvent> : TMessageEventHandler<UsableEvent>
    {
    }
    [EventOrder(1)]
    public class Usable_1_EventHandler<UsableEvent> : TMessageEventHandler<UsableEvent>
    {
    }
    [EventOrder(2)]
    public class Usable_2_EventHandler<UsableEvent> : TMessageEventHandler<UsableEvent>
    {
    }
    [EventOrder(3)]
    public class Usable_3_EventHandler<UsableEvent> : TMessageEventHandler<UsableEvent>
    {
    }
    [EventOrder(4)]
    public class Usable_4_EventHandler<UsableEvent> : TMessageEventHandler<UsableEvent>
    {
    }
    [EventOrder(5)]
    public class Usable_5_EventHandler<UsableEvent> : TMessageEventHandler<UsableEvent>
    {
    }

    [EventTopic("test")]
    public class ExceptionEvent
    {
        public int Id { get; set; }
    }

    [EventOrder(0)]
    public class Exception_0_EventHandler<ExceptionEvent> : TMessageEventHandler<ExceptionEvent>
    {
    }
    [EventOrder(1)]
    public class Exception_1_EventHandler<ExceptionEvent> : TMessageEventHandler<ExceptionEvent>
    {
    }
    [EventOrder(2)]
    public class Exception_2_EventHandler<ExceptionEvent> : TMessageEventHandler<ExceptionEvent>
    {
    }
    [EventOrder(3)]
    public class Exception_3_EventHandler<ExceptionEvent> : TMessageEventHandler<ExceptionEvent>
    {
    }
    [EventOrder(4)]
    public class Exception_4_EventHandler<ExceptionEvent> : TMessageEventHandler<ExceptionEvent>
    {
    }

    [EventOrder(5)]
    public class Exception_5_EventHandler<ExceptionEvent> : TMessageEventHandler<ExceptionEvent>
    {
        public override Task ExecuteAsync(EventBody<ExceptionEvent> message, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}
