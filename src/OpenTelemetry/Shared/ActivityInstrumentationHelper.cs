// <copyright file="ActivityInstrumentationHelper.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1600 // Elements should be documented
#nullable disable

using System;
using System.Diagnostics;

namespace OpenTelemetry.Instrumentation;

/// <summary>
/// Helper.
/// </summary>
internal static class ActivityInstrumentationHelper
{
    internal static readonly Action<Activity, ActivityKind> SetKindProperty = CreateActivityKindSetter();
    internal static readonly Action<Activity, ActivitySource> SetActivitySourceProperty = CreateActivitySourceSetter();

    private static Action<Activity, ActivitySource> CreateActivitySourceSetter()
    {
        return (Action<Activity, ActivitySource>)typeof(Activity).GetProperty("Source")
            .SetMethod.CreateDelegate(typeof(Action<Activity, ActivitySource>));
    }

    private static Action<Activity, ActivityKind> CreateActivityKindSetter()
    {
        return (Action<Activity, ActivityKind>)typeof(Activity).GetProperty("Kind")
            .SetMethod.CreateDelegate(typeof(Action<Activity, ActivityKind>));
    }
}
