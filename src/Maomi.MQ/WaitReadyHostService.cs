using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maomi.MQ;

/// <summary>
/// Wait for all MQ hosts to be ready.
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
