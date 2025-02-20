// <copyright file="MaomiMQDiagnosticListener.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Diagnostics;
using OpenTelemetry.Internal;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;
using System.Reflection;

namespace OpenTelemetry.Instrumentation.MaomiMQ.Implementation;

/// <summary>
/// 监听 Maomi.MQ 中的 Activity，并创建对应的 EventSource，并触发 MaomiMQInstrumentationEventSource.
/// </summary>
internal sealed class MaomiMQDiagnosticListener : ListenerHandler
{
    /// <summary>
    /// OpenTelemetry.Instrumentation.MaomiMQ assembly.
    /// </summary>
    internal static readonly Assembly Assembly = typeof(MaomiMQDiagnosticListener).Assembly;

    /// <summary>
    /// OpenTelemetry.Instrumentation.MaomiMQ assembly version.
    /// </summary>
    internal static readonly AssemblyName AssemblyName = Assembly.GetName();

    /// <summary>
    /// <see cref="DiagnosticName.MaomiMQ"/>.
    /// </summary>
    internal static readonly ActivitySource ActivitySource = new(DiagnosticName.MaomiMQ, Assembly.GetPackageVersion());

    private readonly MaomiMQInstrumentationOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaomiMQDiagnosticListener"/> class.
    /// </summary>
    /// <param name="sourceName"></param>
    /// <param name="options"></param>
    public MaomiMQDiagnosticListener(string sourceName, MaomiMQInstrumentationOptions options)
        : base(sourceName)
    {
        Guard.ThrowIfNull(options);
        this.options = options;
    }

    /// <inheritdoc />
    public override void OnEventWritten(string name, object? payload)
    {
        return;
        //Activity? activity = Activity.Current;
        //Guard.ThrowIfNull(activity);
        //switch (name)
        //{
        //    case DiagnosticName.Event.PublisherStart:
        //        this.OnStartActivity(activity, payload);
        //        break;
        //    case DiagnosticName.Activity.Publisher + ".Stop":
        //        this.OnStopActivity(activity, payload);
        //        break;
        //    case DiagnosticName.Activity.Publisher + ".Execption":
        //        this.OnException(activity, payload);
        //        break;
        //}
    }

    private static ActivityKind GetActivityKind(Activity activity)
    {
        return activity.OperationName switch
        {
            //DiagnosticName.Activity.Publisher => ActivityKind.Producer,
            //DiagnosticName.Activity.Consumer => ActivityKind.Consumer,
            //DiagnosticName.Activity.Fallback => ActivityKind.Consumer,
            //DiagnosticName.Activity.Execute => ActivityKind.Consumer,
            //DiagnosticName.Activity.Retry => ActivityKind.Consumer,
            //DiagnosticName.Activity.EventBus => ActivityKind.Consumer,
            _ => activity.Kind,
        };
    }

    private void OnStartActivity(Activity activity, object? payload)
    {
        if (activity.IsAllDataRequested)
        {
            ActivityInstrumentationHelper.SetActivitySourceProperty(activity, ActivitySource);
            ActivityInstrumentationHelper.SetKindProperty(activity, GetActivityKind(activity));
        }
    }

    private void OnStopActivity(Activity activity, object? payload)
    {
        if (activity.IsAllDataRequested)
        {
            try
            {
                if (payload != null)
                {
                    this.options.Enrich?.Invoke(activity, "OnStopActivity", payload);
                }
            }
            catch (Exception ex)
            {
                MaomiMQInstrumentationEventSource.Log.EnrichmentException(ex);
            }
        }
    }

    private void OnException(Activity activity, object? payload)
    {
        if (activity.IsAllDataRequested)
        {
            var exc = payload as Exception;
            if (exc == null)
            {
                MaomiMQInstrumentationEventSource.Log.NullPayload(nameof(MaomiMQDiagnosticListener), nameof(this.OnStopActivity));
                return;
            }

            if (this.options.RecordException)
            {
                activity.RecordException(exc);
            }

            activity.SetStatus(Status.Error.WithDescription(exc.Message));
        }
    }
}
