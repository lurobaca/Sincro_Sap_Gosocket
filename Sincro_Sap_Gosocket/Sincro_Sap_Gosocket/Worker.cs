// Sincro_Sap_Gosocket/Worker.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OpcionesServicio _opciones;

        public Worker(
            ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<OpcionesServicio> opcionesServicio)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _opciones = opcionesServicio.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pollSeconds = Math.Max(1, _opciones.PollSeconds);
            var batchSize = Math.Max(1, _opciones.BatchSize);

            _logger.LogInformation("Worker iniciado. PollSeconds={PollSeconds}s BatchSize={BatchSize}",
                pollSeconds, batchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Crear scope por ciclo (o por “batch”)
                    using var scope = _scopeFactory.CreateScope();

                    var servicioProcesamiento = scope.ServiceProvider
                        .GetRequiredService<IServicioProcesamientoDocumentos>();

                    // 1) Enviar pendientes (PENDING/RETRY)
                    await servicioProcesamiento.ProcesarPendientesAsync(batchSize, stoppingToken);

                    // 2) Consultar seguimiento Hacienda (WAITING_HACIENDA)
                    //await servicioProcesamiento.ProcesarSeguimientoHaciendaAsync(batchSize, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error general en ciclo del Worker.");
                }

                await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
            }

            _logger.LogInformation("Worker detenido.");
        }
    }
}
