// <copyright file="MaomiMQInstrumentation.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1600 // Elements should be documented

using System;
using Maomi.MQ.Diagnostics;
using OpenTelemetry.Instrumentation.MaomiMQ.Implementation;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.Instrumentation.MaomiMQ;

internal class MaomiMQInstrumentation : IDisposable
{
    private readonly DiagnosticSourceSubscriber diagnosticSourceSubscriber;

    public MaomiMQInstrumentation()
        : this(new MaomiMQInstrumentationOptions())
    {
    }

    public MaomiMQInstrumentation(MaomiMQInstrumentationOptions options)
    {
        this.diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(
            name => new MaomiMQDiagnosticListener(name, options),
            listener => listener.Name == DiagnosticName.MaomiMQ,
            null,
            MaomiMQInstrumentationEventSource.Log.UnknownErrorProcessingEvent);
        this.diagnosticSourceSubscriber.Subscribe();
    }

    public void Dispose()
    {
        this.diagnosticSourceSubscriber?.Dispose();
    }
}