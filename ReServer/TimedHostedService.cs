using System;
using System.Threading;
using System.Threading.Tasks;
using LibToolActions.BackgroundTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReServer.Models;
using SystemToolsShared;

namespace ReServer;

public sealed class TimedHostedService : IHostedService, IDisposable
{
    private static readonly string AppAgentKey =
        "8959D94B-596E-48C1-A644-29667AEE2250" + Environment.MachineName.Capitalize();

    private readonly AppSettings? _appSettings;
    private readonly ILogger<TimedHostedService> _logger;
    private readonly IProcesses _processes;

    private int _executionCount;
    private JobStarter? _jobStarter;
    private Timer? _timer;

    public TimedHostedService(ILogger<TimedHostedService> logger, IProcesses processes,
        IConfiguration configuration)
    {
        _logger = logger;
        _processes = processes;
        var projectSettingsSection = configuration.GetSection("AppSettings");
        _appSettings = projectSettingsSection.Get<AppSettings>();
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");

        // ReSharper disable once DisposableConstructor
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        var count = Interlocked.Increment(ref _executionCount);

        _logger.LogInformation("Timed Hosted Service is working. Count: {Count}, ", count);

        if (_jobStarter == null)
            StartJobs();
        else
            _jobStarter?.DoTimerEventAnswer();
    }

    private void StartJobs()
    {
        _logger.LogInformation("Start Jobs");

        if (string.IsNullOrWhiteSpace(_appSettings?.InstructionsFileName))
        {
            _logger.LogError("InstructionsFileName does not specified in appSettings");
            return;
        }

        //ჯობების ნაწილის გაშვება
        _jobStarter = new JobStarter(_logger, _processes, _appSettings.InstructionsFileName, AppAgentKey);
        _jobStarter.Run();
    }
}