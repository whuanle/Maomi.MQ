// <copyright file="DefaultWaitReadyFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Defaults;

/// <inheritdoc/>
public class DefaultWaitReadyFactory : IWaitReadyFactory
{
    private readonly List<Task> _tasks = new List<Task>();

    /// <inheritdoc/>
    public void AddTask(Task task)
    {
        _tasks.Add(task);
    }

    /// <inheritdoc/>
    public Task WaitReadyAsync()
    {
        return Task.WhenAll(_tasks);
    }
}
