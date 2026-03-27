
using Microsoft.Extensions.Logging;
using SAPbobsCOM;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Dominio.Entidades;
using Sincro_Sap_Gosocket.Dominio.Enumeraciones;
using Sincro_Sap_Gosocket.Infraestructura.Logs;
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

        //   public Task ActualizarEstadoHaciendaAsync(
        //ActualizacionEstadoHacienda actualizacion,
        //CancellationToken cancellationToken = default)
        //   {

        //       TrazaArchivo.Escribir($"Ejecuta ActualizarEstadoHaciendaAsync");

        //       Company? company = null;
        //       Documents? documento = null;

        //       try
        //       {
        //           company = _sapConnectionFactory.CrearConexion();

        //           documento = ObtenerDocumento(company, actualizacion.TipoDocumento);

        //           if (!documento.GetByKey(actualizacion.DocEntry))
        //           {
        //               TrazaArchivo.Escribir($"No se encontró el documento SAP. DocEntry: {actualizacion.DocEntry}");

        //               throw new InvalidOperationException(
        //                   $"No se encontró el documento SAP. DocEntry: {actualizacion.DocEntry}");
        //           }

        //           AsignarUdfSiExiste(documento, actualizacion.CampoEstado, actualizacion.EstadoHacienda);
        //           AsignarUdfSiExiste(documento, actualizacion.CampoMensaje, actualizacion.MensajeHacienda);
        //           AsignarUdfSiExiste(documento, actualizacion.CampoClave, actualizacion.Clave);
        //           AsignarUdfSiExiste(documento, actualizacion.CampoFechaRespuesta, actualizacion.FechaRespuestaTexto);
        //           AsignarUdfSiExiste(documento, actualizacion.Reintenta, "01");

        //           var resultado = documento.Update();
        //           if (resultado != 0)
        //           {                   
        //               company.GetLastError(out int codigo, out string mensaje);

        //               TrazaArchivo.Escribir($"Error actualizando documento SAP. Tipo: {actualizacion.TipoDocumento}, DocEntry: {actualizacion.DocEntry}, Mensaje: {mensaje}");

        //               _logger.LogError(
        //                  "Error actualizando documento SAP. Tipo: {TipoDocumento}, DocEntry: {DocEntry}, Mensaje: {mensaje}",
        //                  actualizacion.TipoDocumento,
        //                  actualizacion.DocEntry,
        //                  mensaje);

        //               throw new InvalidOperationException(
        //                   $"Error actualizando documento SAP. Código: {codigo}. Mensaje: {mensaje}");
        //           }

        //           TrazaArchivo.Escribir($"Documento SAP actualizado correctamente. Tipo: {actualizacion.TipoDocumento}, DocEntry: {actualizacion.DocEntry}, Estado: {actualizacion.EstadoHacienda}");

        //           _logger.LogInformation(
        //               "Documento SAP actualizado correctamente. Tipo: {TipoDocumento}, DocEntry: {DocEntry}, Estado: {Estado}",
        //               actualizacion.TipoDocumento,
        //               actualizacion.DocEntry,
        //               actualizacion.EstadoHacienda);
        //       }
        //       finally
        //       {
        //           LiberarCom(documento);

        //           if (company is not null)
        //           {
        //               if (company.Connected)
        //               {
        //                   company.Disconnect();
        //               }

        //               LiberarCom(company);
        //           }

        //           GC.Collect();
        //           GC.WaitForPendingFinalizers();
        //           GC.Collect();
        //           GC.WaitForPendingFinalizers();
        //       }

        //       return Task.CompletedTask;
        //   }

        //public Task ActualizarEstadoHaciendaEnSapAsync(
        //    ActualizacionEstadoHacienda actualizacion,
        //    CancellationToken cancellationToken = default)
        //{
        //    try {

        //    TrazaArchivo.Escribir("Ejecuta ActualizarEstadoHaciendaEnSapAsync");

        //    var clave = actualizacion.Clave;

        //    string DocNum = string.Empty;

        //    if (!string.IsNullOrEmpty(clave) && clave.Length >= 8)
        //    {
        //        DocNum = clave.Substring(clave.Length - 8);
        //    }

        //    Company? company = null;
        //    Documents? documento = null;

        //    try
        //    {
        //        company = _sapConnectionFactory.CrearConexion();

        //        documento = ObtenerDocumento(company, actualizacion.TipoDocumento);

        //        if (!documento.GetByKey(actualizacion.DocEntry))
        //        {
        //            TrazaArchivo.Escribir(
        //                $"No se encontró el documento SAP. Tipo={actualizacion.TipoDocumento} DocNum={DocNum}");

        //            _logger.LogWarning(
        //                "No se encontró el documento SAP. Tipo: {TipoDocumento}, DocNum: {DocNum}",
        //                actualizacion.TipoDocumento,
        //               DocNum);

        //            return Task.CompletedTask;
        //        }

        //        AsignarUdfSiExiste(documento, actualizacion.CampoEstado, actualizacion.EstadoHacienda);
        //        AsignarUdfSiExiste(documento, actualizacion.CampoMensaje, actualizacion.MensajeHacienda);
        //        AsignarUdfSiExiste(documento, actualizacion.CampoClave, actualizacion.Clave);
        //        AsignarUdfSiExiste(documento, actualizacion.CampoFechaRespuesta, actualizacion.FechaRespuestaTexto);
        //        AsignarUdfSiExiste(documento, actualizacion.Reintenta, "01");

        //        var resultado = documento.Update();
        //        if (resultado != 0)
        //        {
        //            company.GetLastError(out int codigo, out string mensaje);

        //            TrazaArchivo.Escribir(
        //                $"Error actualizando SAP | Tipo={actualizacion.TipoDocumento} | DocNum={DocNum} | Codigo={codigo} | Mensaje={mensaje}");

        //            _logger.LogError(
        //                "Error actualizando documento SAP. Tipo: {TipoDocumento}, DocNum: {DocNum}, Codigo: {Codigo}, Mensaje: {Mensaje}",
        //                actualizacion.TipoDocumento,
        //                DocNum,
        //                codigo,
        //                mensaje);

        //            return Task.CompletedTask;
        //        }

        //        TrazaArchivo.Escribir(
        //            $"Documento SAP actualizado correctamente. Tipo={actualizacion.TipoDocumento}, DocNum={DocNum}, Estado={actualizacion.EstadoHacienda}");

        //        _logger.LogInformation(
        //            "Documento SAP actualizado correctamente. Tipo: {TipoDocumento}, DocNum: {DocNum}, Estado: {EstadoHacienda}",
        //            actualizacion.TipoDocumento,
        //            DocNum,
        //            actualizacion.EstadoHacienda);
        //    }
        //    catch (Exception ex)
        //    {


        //        TrazaArchivo.Escribir(
        //            $"EXCEPCION ActualizarEstadoHaciendaEnSapAsync | Tipo={actualizacion.TipoDocumento} | DocNum={DocNum} | Error={ex.Message}");

        //        _logger.LogError(
        //            ex,
        //            "Excepción actualizando SAP. Tipo: {TipoDocumento}, DocNum: {DocNum}",
        //            actualizacion.TipoDocumento,
        //            DocNum);

        //        return Task.CompletedTask;
        //    }
        //    finally
        //    {
        //        TrazaArchivo.Escribir("FINALLY 1 - Antes LiberarCom(documento)");
        //        LiberarCom(documento);

        //        TrazaArchivo.Escribir("FINALLY 2 - Después LiberarCom(documento)");

        //        if (company is not null)
        //        {
        //            TrazaArchivo.Escribir("FINALLY 3 - company no es null");

        //            if (company.Connected)
        //            {
        //                TrazaArchivo.Escribir("FINALLY 4 - Antes company.Disconnect()");
        //                company.Disconnect();
        //                TrazaArchivo.Escribir("FINALLY 5 - Después company.Disconnect()");
        //            }

        //            TrazaArchivo.Escribir("FINALLY 6 - Antes LiberarCom(company)");
        //            LiberarCom(company);
        //            TrazaArchivo.Escribir("FINALLY 7 - Después LiberarCom(company)");
        //        }

        //        //TrazaArchivo.Escribir("FINALLY 8 - Antes GC.Collect()");
        //        //GC.Collect();
        //        //TrazaArchivo.Escribir("FINALLY 9 - Después GC.Collect()");

        //        //GC.WaitForPendingFinalizers();
        //        //TrazaArchivo.Escribir("FINALLY 10 - Después WaitForPendingFinalizers()");

        //        //GC.Collect();
        //        //TrazaArchivo.Escribir("FINALLY 11 - Después segundo GC.Collect()");

        //        //GC.WaitForPendingFinalizers();
        //        //TrazaArchivo.Escribir("FINALLY 12 - Fin finally");
        //    }

        //    }
        //    catch(Exception ex)
        //    {
        //        TrazaArchivo.Escribir($"ERROR ActualizarEstadoHaciendaEnSapAsync Mensaje={ex.Message}");
        //    }  

        //    return Task.CompletedTask;
        //}

        public Task ActualizarEstadoHaciendaEnSapAsync(
    ActualizacionEstadoHacienda actualizacion,
    CancellationToken cancellationToken = default)
        {
            try
            {
                TrazaArchivo.Escribir("A1 - Entra ActualizarEstadoHaciendaEnSapAsync");

                var clave = actualizacion.Clave;
                TrazaArchivo.Escribir("A2 - Leyó actualizacion.Clave");

                string docNum = string.Empty;

                if (!string.IsNullOrEmpty(clave) && clave.Length >= 8)
                {
                    docNum = clave.Substring(clave.Length - 8);
                }

                TrazaArchivo.Escribir($"A3 - DocNum calculado={docNum}");

                Company? company = null;
                Documents? documento = null;

                try
                {
                    TrazaArchivo.Escribir("A4 - Antes CrearConexion()");
                    company = _sapConnectionFactory.CrearConexion();
                    TrazaArchivo.Escribir("A5 - Después CrearConexion()");

                    TrazaArchivo.Escribir("A6 - Antes ObtenerDocumento()");
                    documento = ObtenerDocumento(company, actualizacion.TipoDocumento);
                    TrazaArchivo.Escribir("A7 - Después ObtenerDocumento()");

                    TrazaArchivo.Escribir($"A8 - Antes GetByKey DocEntry={actualizacion.DocEntry}");
                    var existe = documento.GetByKey(actualizacion.DocEntry);
                    TrazaArchivo.Escribir($"A9 - Después GetByKey resultado={existe}");

                    if (!existe)
                    {
                        TrazaArchivo.Escribir($"A10 - Documento no encontrado Tipo={actualizacion.TipoDocumento} DocNum={docNum}");
                        return Task.CompletedTask;
                    }

                    TrazaArchivo.Escribir("A11 - Antes AsignarUdfSiExiste Estado");
                    AsignarUdfSiExiste(documento, actualizacion.CampoEstado, actualizacion.EstadoHacienda);

                    TrazaArchivo.Escribir("A12 - Antes AsignarUdfSiExiste Mensaje");
                    AsignarUdfSiExiste(documento, actualizacion.CampoMensaje, actualizacion.MensajeHacienda);

                    TrazaArchivo.Escribir("A13 - Antes AsignarUdfSiExiste Clave");
                    AsignarUdfSiExiste(documento, actualizacion.CampoClave, actualizacion.Clave);

                    TrazaArchivo.Escribir("A14 - Antes AsignarUdfSiExiste Fecha");
                    AsignarUdfSiExiste(documento, actualizacion.CampoFechaRespuesta, actualizacion.FechaRespuestaTexto);

                    TrazaArchivo.Escribir("A15 - Antes AsignarUdfSiExiste Reintenta");
                    AsignarUdfSiExiste(documento, actualizacion.Reintenta, "01");

                    TrazaArchivo.Escribir("A16 - Antes documento.Update()");
                    var resultado = documento.Update();
                    TrazaArchivo.Escribir($"A17 - Después documento.Update() resultado={resultado}");

                    if (resultado != 0)
                    {
                        company.GetLastError(out int codigo, out string mensaje);
                        TrazaArchivo.Escribir($"A18 - Error SAP codigo={codigo} mensaje={mensaje}");
                        return Task.CompletedTask;
                    }

                    TrazaArchivo.Escribir($"A19 - Documento SAP actualizado correctamente. Tipo={actualizacion.TipoDocumento}, DocNum={docNum}, Estado={actualizacion.EstadoHacienda}");
                }
                catch (Exception ex)
                {
                    TrazaArchivo.Escribir($"A20 - EXCEPCION INTERNA: {ex}");
                    return Task.CompletedTask;
                }
                finally
                {
                    TrazaArchivo.Escribir("A21 - Finally antes LiberarCom(documento)");
                    LiberarCom(documento);

                    TrazaArchivo.Escribir("A22 - Finally después LiberarCom(documento)");

                    if (company is not null)
                    {
                        TrazaArchivo.Escribir("A23 - Finally company no es null");

                        if (company.Connected)
                        {
                            TrazaArchivo.Escribir("A24 - Antes Disconnect()");
                            company.Disconnect();
                            TrazaArchivo.Escribir("A25 - Después Disconnect()");
                        }

                        TrazaArchivo.Escribir("A26 - Antes LiberarCom(company)");
                        LiberarCom(company);
                        TrazaArchivo.Escribir("A27 - Después LiberarCom(company)");
                    }
                }
            }
            catch (Exception ex)
            {
                TrazaArchivo.Escribir($"A28 - EXCEPCION EXTERNA: {ex}");
            }

            return Task.CompletedTask;
        }
        private static Documents ObtenerDocumento(Company company, string tipoDocumento)
        {             
            return tipoDocumento switch
            {
                // FACTURA ELECTRÓNICA
                "FE" => (Documents)company.GetBusinessObject(BoObjectTypes.oInvoices),

                // NOTA DE CRÉDITO
                "NC" => (Documents)company.GetBusinessObject(BoObjectTypes.oCreditNotes),

                // NOTA DE DÉBITO
                "ND" => (Documents)company.GetBusinessObject(BoObjectTypes.oInvoices),
                // SAP maneja ND como factura con subtipo ND en OINV

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