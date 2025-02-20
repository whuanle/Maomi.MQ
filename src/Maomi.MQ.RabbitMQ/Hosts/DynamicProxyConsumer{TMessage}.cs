// <copyright file="DynamicProxyConsumer{TMessage}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600
#pragma warning disable CS1591

namespace Maomi.MQ.Hosts;

public sealed class DynamicProxyConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
    private readonly ConsumerExecuteAsync<TMessage> _execute;
    private readonly ConsumerFaildAsync<TMessage>? _faild;
    private readonly ConsumerFallbackAsync<TMessage>? _fallback;

    public DynamicProxyConsumer(ConsumerExecuteAsync<TMessage> execute, ConsumerFaildAsync<TMessage>? faild, ConsumerFallbackAsync<TMessage>? fallback)
    {
        _execute = execute;
        _faild = faild;
        _fallback = fallback;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, TMessage message)
    {
        return _execute.Invoke(messageHeader, message);
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TMessage message)
    {
        if (_faild == null)
        {
            return Task.CompletedTask;
        }

        return _faild.Invoke(messageHeader, ex, retryCount, message);
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TMessage? message, Exception? ex)
    {
        if (_fallback == null)
        {
            return Task.FromResult(ConsumerState.Ack);
        }

        return _fallback.Invoke(messageHeader, message, ex);
    }
}