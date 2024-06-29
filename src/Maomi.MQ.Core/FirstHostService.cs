// <copyright file="FirstHostService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ;

/// <summary>
/// Wait for all MQ hosts to be ready.This type must be registered at the end.<br />
/// 用于等待所有队列准备就绪，必须放到最后注册.
/// </summary>
public class FirstHostService : BackgroundService
{
    private readonly IWaitReadyFactory _waitReadyFactory;
    private readonly ILogger<FirstHostService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirstHostService"/> class.
    /// </summary>
    /// <param name="waitReadyFactory"></param>
    /// <param name="logger"></param>
    public FirstHostService(IWaitReadyFactory waitReadyFactory, ILogger<FirstHostService> logger)
    {
        _waitReadyFactory = waitReadyFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Initializing the Maomi.MQ runtime environment!");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
