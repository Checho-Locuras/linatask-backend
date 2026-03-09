using LinaTask.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Infrastructure.Services
{
    public class PaymentAutoReleaseWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentAutoReleaseWorker> _logger;

        public PaymentAutoReleaseWorker(IServiceProvider sp, ILogger<PaymentAutoReleaseWorker> logger)
        {
            _serviceProvider = sp;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //Implementar
                    using var scope = _serviceProvider.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IMarketplacePaymentService>();
                    await svc.ProcessAutoReleasesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PaymentAutoReleaseWorker");
                }

                // Ejecutar cada 10 minutos
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
