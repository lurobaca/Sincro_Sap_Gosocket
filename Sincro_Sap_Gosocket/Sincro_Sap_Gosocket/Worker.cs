using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;

namespace Sincro_Sap_Gosocket
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<OpcionesServicio> _opt;

        public Worker(
            ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<OpcionesServicio> opt)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _opt = opt;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pollSeconds = Math.Max(1, _opt.Value.PollSeconds);
            var batchSize = Math.Max(1, _opt.Value.BatchSize);

            _logger.LogInformation("Worker iniciado. PollSeconds={PollSeconds}, BatchSize={BatchSize}", pollSeconds, batchSize);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(pollSeconds));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IServicioProcesamientoDocumentos>();

                    await svc.ProcesarPendientesAsync(batchSize, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en ciclo del Worker. Se reintentará en el siguiente tick.");
                }
            }

            _logger.LogInformation("Worker detenido.");
        }
    }
}
