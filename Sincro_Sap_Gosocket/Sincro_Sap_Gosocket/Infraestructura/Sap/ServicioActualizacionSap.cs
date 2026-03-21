
using Microsoft.Extensions.Logging;
using SAPbobsCOM;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Dominio.Entidades;
using Sincro_Sap_Gosocket.Dominio.Enumeraciones;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Infraestructura.Sap
{
    public sealed class ServicioActualizacionSap : IServicioActualizacionSap
    {
        private readonly ISapConnectionFactory _sapConnectionFactory;
        private readonly ILogger<ServicioActualizacionSap> _logger;

        public ServicioActualizacionSap(
            ISapConnectionFactory sapConnectionFactory,
            ILogger<ServicioActualizacionSap> logger)
        {
            _sapConnectionFactory = sapConnectionFactory;
            _logger = logger;
        }

        public Task ActualizarEstadoHaciendaAsync(
     ActualizacionEstadoHacienda actualizacion,
     CancellationToken cancellationToken = default)
        {
            Company? company = null;
            Documents? documento = null;

            try
            {
                company = _sapConnectionFactory.CrearConexion();

                documento = ObtenerDocumento(company, actualizacion.TipoDocumento);

                if (!documento.GetByKey(actualizacion.DocEntry))
                {
                    throw new InvalidOperationException(
                        $"No se encontró el documento SAP. DocEntry: {actualizacion.DocEntry}");
                }

                AsignarUdfSiExiste(documento, actualizacion.CampoEstado, actualizacion.EstadoHacienda);
                AsignarUdfSiExiste(documento, actualizacion.CampoMensaje, actualizacion.MensajeHacienda);
                AsignarUdfSiExiste(documento, actualizacion.CampoClave, actualizacion.Clave);
                AsignarUdfSiExiste(documento, actualizacion.CampoFechaRespuesta, actualizacion.FechaRespuestaTexto);
                AsignarUdfSiExiste(documento, actualizacion.Reintenta, "01");

                var resultado = documento.Update();
                if (resultado != 0)
                {
                    company.GetLastError(out int codigo, out string mensaje);
                    throw new InvalidOperationException(
                        $"Error actualizando documento SAP. Código: {codigo}. Mensaje: {mensaje}");
                }

                _logger.LogInformation(
                    "Documento SAP actualizado correctamente. Tipo: {TipoDocumento}, DocEntry: {DocEntry}, Estado: {Estado}",
                    actualizacion.TipoDocumento,
                    actualizacion.DocEntry,
                    actualizacion.EstadoHacienda);
            }
            finally
            {
                LiberarCom(documento);

                if (company is not null)
                {
                    if (company.Connected)
                    {
                        company.Disconnect();
                    }

                    LiberarCom(company);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return Task.CompletedTask;
        }

        private static Documents ObtenerDocumento(Company company, TipoDocumentoSap tipoDocumento)
        {
            return tipoDocumento switch
            {
                TipoDocumentoSap.Factura =>
                    (Documents)company.GetBusinessObject(BoObjectTypes.oInvoices),

                TipoDocumentoSap.NotaCredito =>
                    (Documents)company.GetBusinessObject(BoObjectTypes.oCreditNotes),

                TipoDocumentoSap.NotaDebito =>
                    (Documents)company.GetBusinessObject(BoObjectTypes.oInvoices),
                // Si su nota de débito la maneja como factura subtipo ND en OINV

                _ => throw new NotSupportedException($"Tipo de documento no soportado: {tipoDocumento}")
            };
        }

        private static void AsignarUdfSiExiste(Documents documento, string? nombreCampo, object? valor)
        {
            if (string.IsNullOrWhiteSpace(nombreCampo) || valor is null)
                return;

            try
            {
                var field = documento.UserFields.Fields.Item(nombreCampo);
                if (field != null)
                {
                    field.Value = valor;
                }
            }
            catch
            {
                // No explotar si el UDF no existe.
                // Puede loguearlo si quiere, pero no detener el proceso.
            }
        }

        private static void LiberarCom(object? comObject)
        {
            if (comObject is not null && Marshal.IsComObject(comObject))
            {
                Marshal.ReleaseComObject(comObject);
            }
        }
    }
}