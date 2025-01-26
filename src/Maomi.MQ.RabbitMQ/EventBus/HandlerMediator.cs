// <copyright file="HandlerMediator.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event mediator, used to generate a sequential event execution flow and compensation flow.<br />
/// 事件中介者，用于生成有顺序的事件执行流程和补偿流程.
/// </summary>
/// <typeparam name="TMessage">Event mode.</typeparam>
public class HandlerMediator<TMessage> : IHandlerMediator<TMessage>
    where TMessage : class
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
#pragma warning disable SA1600 // Elements should be documented

    protected static readonly DiagnosticListener _diagnosticListener = new DiagnosticListener(DiagnosticName.Listener.Consumer);
    protected static readonly ActivitySource _activitySource = new ActivitySource(DiagnosticName.ActivitySource.Consumer);

#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释

    private readonly IServiceProvider _serviceProvider;
    private readonly IEventHandlerFactory<TMessage> _eventInfo;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerMediator{TMessage}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="eventInfo"></param>
    /// <param name="loggerFactory"></param>
    public HandlerMediator(IServiceProvider serviceProvider, IEventHandlerFactory<TMessage> eventInfo, ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _eventInfo = eventInfo;
        _logger = loggerFactory.CreateLogger(DiagnosticName.EventBus);
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(MessageHeader messageHeader, TMessage message, CancellationToken cancellationToken)
    {
        Stack<ActivityInit> eventHandlers = new(_eventInfo.Handlers.Count);

        // Build execution flow.
        // 构建执行链.
        // 1 => 2 => 3 =>...
        foreach (var handler in _eventInfo.Handlers)
        {
            ActivityTagsCollection executeTags = new()
            {
                { "event.id", messageHeader.Id },
                { "event.handler.order", handler.Key },
                { "event.handler.name", handler.Value.Name }
            };

            using Activity? activity = _activitySource.StartActivity(name: DiagnosticName.ActivitySource.EventBusExecute, kind: ActivityKind.Internal, tags: executeTags);
            activity?.Start();

            try
            {
                // Forward execution。
                // 正向执行.
                var eventHandler = _serviceProvider.GetRequiredService(handler.Value) as IEventHandler<TMessage>;
                ArgumentNullException.ThrowIfNull(eventHandler);

                eventHandlers.Push(new ActivityInit(activity, eventHandler));

                await eventHandler.ExecuteAsync(message, cancellationToken);
                activity?.Stop();

                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while executing the event,event type:[{Name}], event id:[{Id}]", typeof(TMessage).Name, messageHeader.Id);

                // Rollback.
                // 回滚.
                while (eventHandlers.TryPop(out var eventHandler))
                {
                    try
                    {
                        await eventHandler.EventHandler.CancelAsync(message, cancellationToken);
                        eventHandler.Activity?.AddTag("event.handler.status", "cancel");
                    }
                    finally
                    {
                        eventHandler.Activity?.Stop();
                    }
                }

                throw;
            }
        }
    }

    private class ActivityInit
    {
        public ActivityInit(Activity? activity, IEventHandler<TMessage> eventHandler)
        {
            Activity = activity;
            EventHandler = eventHandler;
        }

        public Activity? Activity { get; init; }

        public IEventHandler<TMessage> EventHandler { get; init; }
    }
}
