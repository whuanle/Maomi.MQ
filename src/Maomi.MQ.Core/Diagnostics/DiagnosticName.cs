// <copyright file="DiagnosticName.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace Maomi.MQ.Diagnostics;

/// <summary>
/// Diagnostic name.
/// </summary>
public static class DiagnosticName
{
    public const string MaomiMQ = "Maomi.MQ";
    public const string EventBus = "Maomi.MQ.EventBus";
    public const string Consumer = "Maomi.MQ.Consumer";
    public const string Publisher = "Maomi.MQ.Publisher";

    public static class Listener
    {
        public const string Publisher = "MaomiMQPublisherHandlerDiagnosticListener";
        public const string Consumer = "MaomiMQConsumerHandlerDiagnosticListener";
    }

    public static class ActivitySource
    {
        public const string Publisher = "Maomi.MQ.Publisher";
        public const string Consumer = "Maomi.MQ.Consumer";

        public const string Fallback = "Maomi.MQ.Fallback";
        public const string Execute = "Maomi.MQ.Execute";
        public const string Retry = "Maomi.MQ.Retry";

        public const string EventBusExecute = "Maomi.MQ.EventBus.Execute";
    }

    public static class Meter
    {
        public const string Publisher = "Maomi.MQ.Publisher";
        public const string Consumer = "Maomi.MQ.Consumer";

        public const string PublisherMessageCount = "maomimq_publisher_message_count";
        public const string PublisherMessageSent = "maomimq_publisher_message_sent";
        public const string PublisherFaildMessageCount = "maomimq_publisher_message_faild_count";
    }

    public static class Event
    {
        public const string PublisherStart = ActivitySource.Publisher + ".Start";
        public const string PublisherStop = ActivitySource.Publisher + ".Stop";
        public const string PublisherExecption = ActivitySource.Publisher + ".Execption";

        public const string ConsumerStart = ActivitySource.Consumer + ".Start";
        public const string ConsumerStop = ActivitySource.Consumer + ".Stop";
        public const string ConsumerExecption = ActivitySource.Consumer + ".Execption";

        public const string FallbackStart = "Maomi.MQ.Fallback" + ".Start";
        public const string FallbackStop = "Maomi.MQ.Fallback" + ".Stop";
        public const string FallbackExecption = "Maomi.MQ.Fallback" + ".Execption";
    }
}
