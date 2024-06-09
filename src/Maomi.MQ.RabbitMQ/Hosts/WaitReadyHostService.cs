// <copyright file="WaitReadyHostService.cs" company="Maomi">
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
public class WaitReadyHostService : BackgroundService
{
    private readonly IWaitReadyFactory _waitReadyFactory;
    private readonly ILogger<WaitReadyHostService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitReadyHostService"/> class.
    /// </summary>
    /// <param name="waitReadyFactory"></param>
    /// <param name="logger"></param>
    public WaitReadyHostService(IWaitReadyFactory waitReadyFactory, ILogger<WaitReadyHostService> logger)
    {
        _waitReadyFactory = waitReadyFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("The Maomi.MQ service is ready!");
        await _waitReadyFactory.WaitReady();
    }

    /// <inheritdoc/>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
