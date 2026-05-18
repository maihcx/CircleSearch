using Microsoft.Extensions.Hosting;

namespace CircleSearch.Core
{
    public class CoreBackgroundService : BackgroundService
    {
        private readonly Bootstrap bootstrap;

        public CoreBackgroundService(Bootstrap bootstrap)
        {
            this.bootstrap = bootstrap;
            AppRuntime.bootstrap = bootstrap;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bootstrap.OnStarted();

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            bootstrap.OnStopped();

            return base.StopAsync(cancellationToken);
        }
    }
}