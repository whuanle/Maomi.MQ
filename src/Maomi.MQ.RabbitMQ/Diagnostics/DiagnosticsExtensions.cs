// <copyright file="DiagnosticsExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Maomi.MQ.Diagnostics;

public static class DiagnosticsExtensions
{
#if NET7_0_OR_GREATER && !NET9_0_OR_GREATER
    // see https://github.com/dotnet/runtime/blob/692a3b6a9827fa10c51ce2a16b26b51ecca7b430/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/Activity.cs#L552

    /// <summary>
    /// Add an <see cref="ActivityEvent" /> object containing the exception information to the <see cref="Activity.Events" /> list.
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="exception">The exception to add to the attached events list.</param>
    /// <param name="tags">The tags to add to the exception event.</param>
    /// <param name="timestamp">The timestamp to add to the exception event.</param>
    /// <returns><see langword="this" /> for convenient chaining.</returns>
    public static Activity AddException(this Activity activity, Exception exception, in TagList tags = default, DateTimeOffset timestamp = default)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        TagList exceptionTags = tags;

        const string ExceptionEventName = "exception";
        const string ExceptionMessageTag = "exception.message";
        const string ExceptionStackTraceTag = "exception.stacktrace";
        const string ExceptionTypeTag = "exception.type";

        bool hasMessage = false;
        bool hasStackTrace = false;
        bool hasType = false;

        for (int i = 0; i < exceptionTags.Count; i++)
        {
            if (exceptionTags[i].Key == ExceptionMessageTag)
            {
                hasMessage = true;
            }
            else if (exceptionTags[i].Key == ExceptionStackTraceTag)
            {
                hasStackTrace = true;
            }
            else if (exceptionTags[i].Key == ExceptionTypeTag)
            {
                hasType = true;
            }
        }

        if (!hasMessage)
        {
            exceptionTags.Add(new KeyValuePair<string, object?>(ExceptionMessageTag, exception.Message));
        }

        if (!hasStackTrace)
        {
            exceptionTags.Add(new KeyValuePair<string, object?>(ExceptionStackTraceTag, exception.ToString()));
        }

        if (!hasType)
        {
            exceptionTags.Add(new KeyValuePair<string, object?>(ExceptionTypeTag, exception.GetType().ToString()));
        }

        ActivityTagsCollection tagsCollection = new ActivityTagsCollection(exceptionTags);
        return activity.AddEvent(new ActivityEvent(ExceptionEventName, timestamp, tagsCollection));
    }
#endif

}