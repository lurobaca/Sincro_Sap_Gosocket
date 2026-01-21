using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    public class ServicioProcesamientoDocumentos : IServicioProcesamientoDocumentos
    {
        private const string STATUS_WAITING_HACIENDA = "WAITING_HACIENDA";

        private static readonly HashSet<string> EstadosFinalesHacienda =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "ACEPTADO",
                "RECHAZADO",
                "BAD_REQUEST"
            };

        private readonly ILogger<ServicioProcesamientoDocumentos> _logger;
        private readonly IRepositorioColaDocumentos _repositorioCola;
        private readonly IRepositorioEstados _repositorioEstados;
        private readonly IClienteGosocket _clienteGosocket;

        public ServicioProcesamientoDocumentos(
            ILogger<ServicioProcesamientoDocumentos> logger,
            IRepositorioColaDocumentos repositorioCola,
            IRepositorioEstados repositorioEstados,
            IClienteGosocket clienteGosocket)
        {
            _logger = logger;
            _repositorioCola = repositorioCola;
            _repositorioEstados = repositorioEstados;
            _clienteGosocket = clienteGosocket;
        }

        /// <summary>
        /// Envía documentos pendientes a GoSocket usando el XML ya generado.
        /// </summary>
        public async Task ProcesarPendientesAsync(int batchSize, CancellationToken ct)
        {
            var pendientes = await _repositorioCola.ObtenerPendientesAsync(batchSize, ct);
            if (pendientes.Count == 0) return;

            foreach (var doc in pendientes)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    if (string.IsNullOrWhiteSpace(doc.XmlFilePath) || !File.Exists(doc.XmlFilePath))
                    {
                        await _repositorioEstados.MarcarRetryOFalloAsync(
                            doc.DocumentosPendientes_Id,
                            "XML no existe o ruta inválida.",
                            10,
                            ct);
                        continue;
                    }

                    // 1) Leer XML existente
                    var xml = await File.ReadAllTextAsync(doc.XmlFilePath, ct);

                    // 2) Construir petición GoSocket
                    var peticion = new PeticionSendDocumentToAuthority
                    {
                        DocumentoXml = xml,
                        TipoDocumento = doc.TipoCE ?? string.Empty,
                        CodigoPais = "CR",
                        Asincrono = true
                    };

                    // 3) Enviar
                    var respuesta = await _clienteGosocket.EnviarDocumentoAutoridadAsync(peticion, ct);

                    var json = JsonSerializer.Serialize(respuesta);
                    var trackId = TryParseTrackIdDesdeJson(json);

                    await _repositorioEstados.MarcarWaitingHaciendaAsync(
                        doc.DocumentosPendientes_Id,
                        trackId,
                        null,
                        json,
                        ct);

                    _logger.LogInformation(
                        "Documento enviado a GoSocket. DocId={DocId} TrackId={TrackId}",
                        doc.DocumentosPendientes_Id,
                        trackId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando documento. DocId={DocId}", doc.DocumentosPendientes_Id);

                    await _repositorioEstados.MarcarRetryOFalloAsync(
                        doc.DocumentosPendientes_Id,
                        ex.Message,
                        10,
                        ct);
                }
            }
        }

        /// <summary>
        /// Consulta estados de Hacienda vía GoSocket.
        /// </summary>
        public async Task ProcesarSeguimientoHaciendaAsync(int batchSize, CancellationToken ct)
        {
            var pendientes = await _repositorioCola.ObtenerPendientesSeguimientoAsync(
                STATUS_WAITING_HACIENDA,
                batchSize,
                ct);

            foreach (var doc in pendientes)
            {
                if (string.IsNullOrWhiteSpace(doc.GoSocket_TrackId))
                    continue;

                try
                {
                    var peticion = new PeticionGetDocument
                    {
                        CodigoDocumento = doc.GoSocket_TrackId,
                        CodigoPais = "CR"
                    };

                    var respuesta = await _clienteGosocket.ObtenerDocumentoAsync(peticion, ct);
                    var json = JsonSerializer.Serialize(respuesta);

                    var estado = TryParseEstadoHaciendaDesdeJson(json);
                    var esFinal = EstadosFinalesHacienda.Contains(estado ?? string.Empty);

                    if (esFinal)
                    {
                        await _repositorioEstados.MarcarDoneAsync(
                            doc.DocumentosPendientes_Id,
                            ct);
                    }
                    else
                    {
                        await _repositorioEstados.ActualizarSeguimientoHaciendaAsync(
                            doc.DocumentosPendientes_Id,
                            estado,
                            json,
                            esFinal,
                            10,
                            ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en seguimiento. DocId={DocId}", doc.DocumentosPendientes_Id);

                    await _repositorioEstados.MarcarRetryOFalloAsync(
                        doc.DocumentosPendientes_Id,
                        ex.Message,
                        10,
                        ct);
                }
            }
        }

        private static string? TryParseTrackIdDesdeJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("trackId", out var v)
                    ? v.GetString()
                    : null;
            }
            catch { return null; }
        }

        private static string? TryParseEstadoHaciendaDesdeJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("estado", out var v)
                    ? v.GetString()
                    : null;
            }
            catch { return null; }
        }
    }
}
