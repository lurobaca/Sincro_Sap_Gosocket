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
using Sincro_Sap_Gosocket.Infraestructura.Logs;

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
        
        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{

        //    try { 
        //        var pollSeconds = Math.Max(1, _opciones.PollSeconds);
        //        var batchSize = Math.Max(1, _opciones.BatchSize);

        //        //TrazaArchivo.Escribir($"MARCA APPSETTINGS: {_opciones.MarcaServidor}");
        //        //_logger.LogInformation("MARCA APPSETTINGS: {Marca}", _opciones.MarcaServidor);

        //        //_logger.LogInformation("Worker iniciado. PollSeconds={PollSeconds}s BatchSize={BatchSize}",
        //        //    pollSeconds, batchSize);

        //        //TrazaArchivo.Escribir($"WORKER INICIADO | PollSeconds={pollSeconds} | BatchSize={batchSize}");

        //        while (!stoppingToken.IsCancellationRequested)
        //        {
        //            try
        //            {
        //                //TrazaArchivo.Escribir("INICIO DE CICLO");

        //                using var scope = _scopeFactory.CreateScope();
        //                //TrazaArchivo.Escribir("SCOPE CREADO");

        //                var servicioProcesamiento = scope.ServiceProvider
        //                    .GetRequiredService<IServicioProcesamientoDocumentos>();

        //                //TrazaArchivo.Escribir("SERVICIO DE PROCESAMIENTO RESUELTO");

        //               await servicioProcesamiento.ProcesarPendientesAsync(batchSize, stoppingToken);

        //                //TrazaArchivo.Escribir("FIN ProcesarPendientesAsync");

        //                //TrazaArchivo.Escribir("Inicia ProcesarSeguimientoHaciendaAsync");
        //                // 2) Consultar seguimiento Hacienda (WAITING_HACIENDA)
        //                await servicioProcesamiento.ProcesarSeguimientoHaciendaAsync(batchSize, stoppingToken);

        //                //TrazaArchivo.Escribir("Fin ProcesarSeguimientoHaciendaAsync");
        //            }
        //            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        //            {
        //                TrazaArchivo.Escribir("WORKER CANCELADO");
        //                break;
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError(ex, "Error general en ciclo del Worker.");
        //                TrazaArchivo.Escribir($"Worker.ExecuteAsync Detalle:{ex.Message}");
        //            }

        //            //TrazaArchivo.Escribir($"ESPERANDO {pollSeconds} SEGUNDOS");
        //            await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
        //        }

        //        _logger.LogInformation("Worker detenido.");
        //        TrazaArchivo.Escribir("WORKER DETENIDO");

        //    }
        //    catch (Exception ex)
        //    {
        //        TrazaArchivo.Escribir($"Worker.ExecuteAsync Error al iniciar: {ex.Message}");
        //        _logger.LogError(ex, "Error al iniciar el Worker.");
               
        //        return; // No continuar si hay error en la inicialización
        //    }
        //}

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pollSeconds = Math.Max(1, _opciones.PollSeconds);
            var batchSize = Math.Max(1, _opciones.BatchSize);

            _logger.LogInformation(
                "Worker iniciado. PollSeconds={PollSeconds}s BatchSize={BatchSize}",
                pollSeconds, batchSize);

            TrazaArchivo.Escribir(
                $"WORKER INICIADO | PollSeconds={pollSeconds} | BatchSize={batchSize}");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        //TrazaArchivo.Escribir("INICIO DE CICLO");

                        using var scope = _scopeFactory.CreateScope();
                        //TrazaArchivo.Escribir("SCOPE CREADO");

                        var servicioProcesamiento = scope.ServiceProvider
                            .GetRequiredService<IServicioProcesamientoDocumentos>();

                        //TrazaArchivo.Escribir("SERVICIO DE PROCESAMIENTO RESUELTO");

                        //TrazaArchivo.Escribir("ANTES ProcesarPendientesAsync");
                        await servicioProcesamiento.ProcesarPendientesAsync(batchSize, stoppingToken);
                        //TrazaArchivo.Escribir("DESPUES ProcesarPendientesAsync");

                        //TrazaArchivo.Escribir("ANTES ProcesarSeguimientoHaciendaAsync");
                        await servicioProcesamiento.ProcesarSeguimientoHaciendaAsync(batchSize, stoppingToken);
                        //TrazaArchivo.Escribir("DESPUES ProcesarSeguimientoHaciendaAsync");

                        //TrazaArchivo.Escribir($"ESPERANDO {pollSeconds} SEGUNDOS");
                        await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Worker cancelado por token.");
                        TrazaArchivo.Escribir("WORKER CANCELADO POR TOKEN");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error general en ciclo del Worker.");
                        TrazaArchivo.Escribir($"Worker.ExecuteAsync ERROR CICLO: {ex}");

                        // Opcional: pequeña pausa para evitar ciclo de error agresivo
                        try
                        {
                            //TrazaArchivo.Escribir("PAUSA DE RECUPERACION 5 SEGUNDOS");
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Worker cancelado durante pausa de recuperación.");
                            TrazaArchivo.Escribir("WORKER CANCELADO DURANTE PAUSA DE RECUPERACION");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error fatal no controlado en ExecuteAsync.");
                TrazaArchivo.Escribir($"Worker.ExecuteAsync ERROR FATAL: {ex}");
                throw;
            }
            finally
            {
                _logger.LogInformation("Worker detenido.");
                TrazaArchivo.Escribir("WORKER DETENIDO");
            }
        }
    }
}
