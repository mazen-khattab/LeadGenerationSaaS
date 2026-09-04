using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Settings;
using SaaS.Application.Features.Worker.Commands.ProcessJobTimeouts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Infrastructure.Services
{
    public class JobTimeoutWatchdogService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JobTimeoutWatchdogService> _logger;
        private readonly JobWatchdogOptions _options;

        public JobTimeoutWatchdogService(IServiceScopeFactory scopeFactory, ILogger<JobTimeoutWatchdogService> logger, IOptions<JobWatchdogOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(_options.CheckIntervalMinutes);

            _logger.LogInformation("JobTimeoutWatchdogService is starting. CheckInterval={Interval}m, TimeoutThreshold={Threshold}m", 
                _options.CheckIntervalMinutes, _options.TimeoutThresholdMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    await mediator.Send(new ProcessJobTimeoutsCommand(), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing JobTimeoutWatchdogService.");
                }

                await Task.Delay(interval, stoppingToken);
            }

            _logger.LogInformation("JobTimeoutWatchdogService is stopping.");
        }
    }
}
