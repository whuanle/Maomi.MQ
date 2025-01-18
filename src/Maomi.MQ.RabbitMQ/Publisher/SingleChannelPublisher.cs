// <copyright file="SingleChannelPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable CS1591
#pragma warning disable SA1401
#pragma warning disable SA1600

using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// <inheritdoc cref="DefaultMessagePublisher"/>
/// </summary>
public class SingleChannelPublisher : DefaultMessagePublisher, IMessagePublisher, ISingleChannelPublisher, IDisposable
{
    protected readonly Lazy<IChannel> _channel;
    protected readonly CreateChannelOptions _createChannelOptions;

    protected bool disposedValue = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleChannelPublisher"/> class.
    /// </summary>
    /// <param name="publisher"></param>
    /// <param name="createChannelOptions"></param>
    internal SingleChannelPublisher(DefaultMessagePublisher publisher, CreateChannelOptions? createChannelOptions = null)
        : base(publisher)
    {
        if (createChannelOptions == null)
        {
            createChannelOptions = new CreateChannelOptions(publisherConfirmationsEnabled: false, publisherConfirmationTrackingEnabled: false);
        }

        _createChannelOptions = createChannelOptions;

        _channel = new Lazy<IChannel>(() =>
        {
            return _connectionObject.Connection.CreateChannelAsync(_createChannelOptions).Result;
        });
    }

    public override Task CustomPublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = null)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        return PublishChannelAsync(_channel.Value, exchange, routingKey, message, properties);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _channel.Value.Dispose();
            }

            disposedValue = true;
        }
    }
}