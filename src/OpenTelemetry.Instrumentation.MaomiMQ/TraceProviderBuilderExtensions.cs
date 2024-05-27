// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Instrumentation.MaomiMQ;
using OpenTelemetry.Internal;
using RabbitMQ.Client;
using System;

// ReSharper disable once CheckNamespace
namespace OpenTelemetry.Trace;

/// <summary>
/// Extension methods to simplify registering of dependency instrumentation.
/// </summary>
public static class TraceProviderBuilderExtensions
{
    /// <summary>
    /// Enables the Quartz.NET Job automatic data collection for Quartz.NET.
    /// </summary>
    /// <param name="builder"><see cref="TraceProviderBuilderExtensions"/> being configured.</param>
    /// <returns>The instance of <see cref="TraceProviderBuilderExtensions"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddMaomiMQInstrumentation(
        this TracerProviderBuilder builder) => AddMaomiMQInstrumentation(builder, configure: null);

    /// <summary>
    /// Enables the Quartz.NET Job automatic data collection for Quartz.NET.
    /// </summary>
    /// <param name="builder"><see cref="TraceProviderBuilderExtensions"/> being configured.</param>
    /// <param name="configure">Quartz configuration options.</param>
    /// <returns>The instance of <see cref="TraceProviderBuilderExtensions"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddMaomiMQInstrumentation(
        this TracerProviderBuilder builder,
        Action<MaomiMQInstrumentationOptions>? configure)
    {
        Guard.ThrowIfNull(builder);

        var options = new MaomiMQInstrumentationOptions();
        configure?.Invoke(options);

        builder.AddInstrumentation(() => new Instrumentation.MaomiMQ.MaomiMQInstrumentation(options));

        builder.AddSource(Maomi.MQ.Diagnostics.DiagnosticName.MaomiMQ);

        builder.AddLegacySource(RabbitMQActivitySource.PublisherSourceName);
        builder.AddLegacySource(RabbitMQActivitySource.SubscriberSourceName);

        return builder;
    }
}
