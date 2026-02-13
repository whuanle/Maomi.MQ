// <copyright file="MQTransactionOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using System.Data.Common;
using System.Text.Json;

namespace Maomi.MQ.Transaction.Models;

/// <summary>
/// Options.
/// </summary>
public interface IMQTransactionOptions
{
    /// <summary>
    /// Create database connection.
    /// </summary>
    public Func<IServiceProvider, DbConnection> Connection { get; }

    /// <summary>
    /// Json serializer options for serializing and deserializing message properties.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    /// Publisher options.
    /// </summary>
    public MQPublisherTransactionOptions Publisher { get; }

    /// <summary>
    /// Consumer options.
    /// </summary>
    public MQConsumerTransactionOptions Consumer { get; }

    /// <summary>
    /// Automatically create table.
    /// </summary>
    public bool AutoCreateTable { get; }
}

/// <summary>
/// Options.
/// </summary>
public class MQTransactionOptions : IMQTransactionOptions
{
    /// <summary>
    /// Create database connection.
    /// </summary>
    public Func<IServiceProvider, DbConnection> Connection { get; set; } = default!;

    /// <summary>
    /// Json serializer options for serializing and deserializing message properties.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new();

    /// <summary>
    /// Publisher options.
    /// </summary>
    public MQPublisherTransactionOptions Publisher { get; set; } = new MQPublisherTransactionOptions
    {
        TableName = "mq_publisher",
        DisplayMessageText = false,
        MaxRetry = 5,
        ScanDbInterval = TimeSpan.FromSeconds(1),
        RetryInterval = (serviceProvider, queue, messageHeader, retryCount) =>
        {
            // 1,2,4,8.16,29
            if (retryCount <= 1)
            {
                return TimeSpan.FromSeconds(1);
            }
            else if (retryCount <= 2)
            {
                return TimeSpan.FromSeconds(2);
            }
            else if (retryCount <= 3)
            {
                return TimeSpan.FromSeconds(4);
            }
            else if (retryCount <= 4)
            {
                return TimeSpan.FromSeconds(8);
            }
            else if (retryCount <= 5)
            {
                return TimeSpan.FromSeconds(16);
            }

            // The timeout for most transaction connections is 30 seconds.
            return TimeSpan.FromSeconds(29);
        }
    };

    /// <summary>
    /// Consumer options.
    /// </summary>
    public MQConsumerTransactionOptions Consumer { get; set; } = new MQConsumerTransactionOptions
    {
        TableName = "mq_consumer"
    };

    /// <summary>
    /// Automatically create table.
    /// </summary>
    public bool AutoCreateTable { get; set; } = false;
}

/// <summary>
/// Retry policy factory delegate.
/// </summary>
/// <param name="serviceProvider"></param>
/// <param name="queue"></param>
/// <param name="messageHeader"></param>
/// <param name="retryCount"></param>
/// <returns>The waiting time for retrying.</returns>
public delegate TimeSpan RetryPolicyFactory(IServiceProvider serviceProvider, string queue, MessageHeader messageHeader, int retryCount);
