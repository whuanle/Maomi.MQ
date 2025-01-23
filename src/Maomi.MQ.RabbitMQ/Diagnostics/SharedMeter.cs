// <copyright file="SharedMeter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591

using System.Diagnostics.Metrics;

namespace Maomi.MQ.Diagnostics;

public sealed class SharedMeter : Meter
{
    public static Meter Publisher { get; } = new SharedMeter(DiagnosticName.Meter.Publisher);

    public static Meter Consumer { get; } = new SharedMeter(DiagnosticName.Meter.Consumer);

    private SharedMeter(string name)
        : base(name)
    {
    }

    protected override void Dispose(bool disposing)
    {
    }
}
