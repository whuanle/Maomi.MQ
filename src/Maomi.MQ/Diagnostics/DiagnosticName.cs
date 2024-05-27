// <copyright file="DiagnosticName.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1600

namespace Maomi.MQ.Diagnostics;

/// <summary>
/// Diagnostic name.
/// </summary>
public static class DiagnosticName
{
    /// <summary>
    /// Maomi.MQ.
    /// </summary>
    public const string MaomiMQ = "Maomi.MQ";

    public static class Tag
    {
        public const string Status = nameof(Status);

        public const string ACK = "ack";
        public const string NACK = "nack";
        public const string Requeue = nameof(Requeue);
    }

    public static class Activity
    {
        public const string Publisher = nameof(Publisher);
        public const string Consumer = nameof(Consumer);
        public const string Fallback = nameof(Fallback);
        public const string Execute = nameof(Execute);
        public const string Retry = nameof(Retry);
        public const string Eventbus = nameof(EventBus);
    }

    public static class Event
    {
        public const string Id = "event.id";
        public const string CreateTime = "event.starttime";
        public const string Queue = "event.queue";

        public const string FallbackCompleted = nameof(FallbackCompleted);
        public const string ExecuteCompleted = nameof(ExecuteCompleted);
        public const string RetryCompleted = nameof(RetryCompleted);
        public const string Retry = nameof(Retry);

        public const string Exception = "Exception";
    }

    public static class HandlerMediator
    {
        public const string Eventbus = "eventbus";
        public const string Execute = "eventbus.execute";
        public const string ExecuteExcetion = "eventbus.exception";
        public const string Cancel = "eventbus.cancel";
    }
}
