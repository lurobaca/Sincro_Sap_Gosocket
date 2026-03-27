using Microsoft.Extensions.Options;
using SAPbobsCOM;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Infraestructura.Logs;
using System;

namespace Sincro_Sap_Gosocket.Infraestructura.Sap
{
    public sealed class SapConnectionFactory : ISapConnectionFactory
    {
        private readonly OpcionesSap _opciones;

        public SapConnectionFactory(IOptions<OpcionesSap> opciones)
        {
            _opciones = opciones.Value;
        }

        //public Company CrearConexion()
        //{
        //    var company = new Company
        //    {
        //        Server = _opciones.Servidor,
        //        LicenseServer = _opciones.LicenciaServidor,
        //        CompanyDB = _opciones.BaseDatos,
        //        UserName = _opciones.Usuario,
        //        Password = _opciones.Clave,
        //        DbUserName = _opciones.UsuarioDb,
        //        DbPassword = _opciones.ClaveDb,
        //        UseTrusted = _opciones.UsarTrusted,
        //        language = (BoSuppLangs)_opciones.Lenguaje,
        //        DbServerType = ObtenerTipoServidor(_opciones.TipoServidorSap)
        //    };
        //    TrazaArchivo.Escribir(
        //                 "SAP CONEXION INTENTO | " +
        //                 $"Server={company.Server} | " +
        //                 $"LicenseServer={company.LicenseServer} | " +
        //                 $"CompanyDB={company.CompanyDB} | " +
        //                 $"UserName={company.UserName} | " +
        //                 $"Password={(string.IsNullOrWhiteSpace(company.Password) ? "(vacío)" : "*****")} | " +
        //                 $"DbUserName={company.DbUserName} | " +
        //                 $"DbPassword={(string.IsNullOrWhiteSpace(company.DbPassword) ? "(vacío)" : "*****")} | " +
        //                 $"UseTrusted={company.UseTrusted} | " +
        //                 $"Language={(int)company.language} | " +
        //                 $"DbServerType={company.DbServerType}"
        //             );
        //    var resultado = company.Connect();
        //    if (resultado != 0)
        //    {
        //        company.GetLastError(out int codigo, out string mensaje);

        //        TrazaArchivo.Escribir(
        //            "SAP CONEXION ERROR | " +
        //            $"Codigo={codigo} | " +
        //            $"Mensaje={mensaje} | " +
        //            $"Server={company.Server} | " +
        //            $"LicenseServer={company.LicenseServer} | " +
        //            $"CompanyDB={company.CompanyDB} | " +
        //            $"UserName={company.UserName} | " +
        //            $"Password={(string.IsNullOrWhiteSpace(company.Password) ? "(vacío)" : "*****")} | " +
        //            $"DbUserName={company.DbUserName} | " +
        //            $"DbPassword={(string.IsNullOrWhiteSpace(company.DbPassword) ? "(vacío)" : "*****")} | " +
        //            $"UseTrusted={company.UseTrusted} | " +
        //            $"Language={(int)company.language} | " +
        //            $"DbServerType={company.DbServerType}"
        //        );

        //        throw new InvalidOperationException(
        //            $"No fue posible conectar a SAP Business One. Código: {codigo}. Mensaje: {mensaje}");
        //    }

        //    return company;
        //}

        public Company CrearConexion()
        {
            Company company = null;

            try
            {
                TrazaArchivo.Escribir("SAP A1 - Antes new Company()");
                company = new Company();
                TrazaArchivo.Escribir("SAP A2 - Después new Company()");

                TrazaArchivo.Escribir("SAP A3 - Antes Server");
                company.Server = _opciones.Servidor;
                TrazaArchivo.Escribir("SAP A4 - Después Server");

                TrazaArchivo.Escribir("SAP A5 - Antes LicenseServer");
                company.LicenseServer = _opciones.LicenciaServidor;
                TrazaArchivo.Escribir("SAP A6 - Después LicenseServer");

                TrazaArchivo.Escribir("SAP A7 - Antes CompanyDB");
                company.CompanyDB = _opciones.BaseDatos;
                TrazaArchivo.Escribir("SAP A8 - Después CompanyDB");

                TrazaArchivo.Escribir("SAP A9 - Antes UserName");
                company.UserName = _opciones.Usuario;
                TrazaArchivo.Escribir("SAP A10 - Después UserName");

                TrazaArchivo.Escribir("SAP A11 - Antes Password");
                company.Password = _opciones.Clave;
                TrazaArchivo.Escribir("SAP A12 - Después Password");

                TrazaArchivo.Escribir("SAP A13 - Antes DbUserName");
                company.DbUserName = _opciones.UsuarioDb;
                TrazaArchivo.Escribir("SAP A14 - Después DbUserName");

                TrazaArchivo.Escribir("SAP A15 - Antes DbPassword");
                company.DbPassword = _opciones.ClaveDb;
                TrazaArchivo.Escribir("SAP A16 - Después DbPassword");

                TrazaArchivo.Escribir("SAP A17 - Antes UseTrusted");
                company.UseTrusted = _opciones.UsarTrusted;
                TrazaArchivo.Escribir("SAP A18 - Después UseTrusted");

                TrazaArchivo.Escribir("SAP A19 - Antes language");
                company.language = (BoSuppLangs)_opciones.Lenguaje;
                TrazaArchivo.Escribir("SAP A20 - Después language");

                TrazaArchivo.Escribir("SAP A21 - Antes DbServerType");
                company.DbServerType = ObtenerTipoServidor(_opciones.TipoServidorSap);
                TrazaArchivo.Escribir("SAP A22 - Después DbServerType");

                TrazaArchivo.Escribir("SAP A23 - Antes Connect()");
                var resultado = company.Connect();
                TrazaArchivo.Escribir($"SAP A24 - Después Connect() resultado={resultado}");

                if (resultado != 0)
                {
                    company.GetLastError(out int codigo, out string mensaje);
                    TrazaArchivo.Escribir($"SAP A25 - Error SAP Codigo={codigo} Mensaje={mensaje}");
                    throw new InvalidOperationException(
                        $"No fue posible conectar a SAP Business One. Código: {codigo}. Mensaje: {mensaje}");
                }

                TrazaArchivo.Escribir("SAP A26 - Conexión SAP exitosa");
                return company;
            }
            catch (Exception ex)
            {
                TrazaArchivo.Escribir($"SAP ERROR EXCEPCION | Tipo={ex.GetType().FullName} | Mensaje={ex.Message} | Stack={ex.StackTrace}");
                throw;
            }
        }
        private static BoDataServerTypes ObtenerTipoServidor(string tipoServidorSap)
        {
            return tipoServidorSap?.ToUpperInvariant() switch
            {
                "MSSQL2008" => BoDataServerTypes.dst_MSSQL2008,
                "MSSQL2012" => BoDataServerTypes.dst_MSSQL2012,
                "MSSQL2014" => BoDataServerTypes.dst_MSSQL2014,
                "MSSQL2016" => BoDataServerTypes.dst_MSSQL2016,
                "MSSQL2017" => BoDataServerTypes.dst_MSSQL2017,
                "MSSQL2019" => BoDataServerTypes.dst_MSSQL2019,
                _ => BoDataServerTypes.dst_MSSQL2019
            };
        }
    }
}