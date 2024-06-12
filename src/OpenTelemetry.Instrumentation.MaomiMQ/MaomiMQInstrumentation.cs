// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Maomi.MQ.Diagnostics;
using OpenTelemetry.Instrumentation.MaomiMQ.Implementation;

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
