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

        public Company CrearConexion()
        {
            var company = new Company
            {
                Server = _opciones.Servidor,
                LicenseServer = _opciones.LicenciaServidor,
                CompanyDB = _opciones.BaseDatos,
                UserName = _opciones.Usuario,
                Password = _opciones.Clave,
                DbUserName = _opciones.UsuarioDb,
                DbPassword = _opciones.ClaveDb,
                UseTrusted = _opciones.UsarTrusted,
                language = (BoSuppLangs)_opciones.Lenguaje,
                DbServerType = ObtenerTipoServidor(_opciones.TipoServidorSap)
            };
            TrazaArchivo.Escribir(
                         "SAP CONEXION INTENTO | " +
                         $"Server={company.Server} | " +
                         $"LicenseServer={company.LicenseServer} | " +
                         $"CompanyDB={company.CompanyDB} | " +
                         $"UserName={company.UserName} | " +
                         $"Password={(string.IsNullOrWhiteSpace(company.Password) ? "(vacío)" : "*****")} | " +
                         $"DbUserName={company.DbUserName} | " +
                         $"DbPassword={(string.IsNullOrWhiteSpace(company.DbPassword) ? "(vacío)" : "*****")} | " +
                         $"UseTrusted={company.UseTrusted} | " +
                         $"Language={(int)company.language} | " +
                         $"DbServerType={company.DbServerType}"
                     );
            var resultado = company.Connect();
            if (resultado != 0)
            {
                company.GetLastError(out int codigo, out string mensaje);

                TrazaArchivo.Escribir(
                    "SAP CONEXION ERROR | " +
                    $"Codigo={codigo} | " +
                    $"Mensaje={mensaje} | " +
                    $"Server={company.Server} | " +
                    $"LicenseServer={company.LicenseServer} | " +
                    $"CompanyDB={company.CompanyDB} | " +
                    $"UserName={company.UserName} | " +
                    $"Password={(string.IsNullOrWhiteSpace(company.Password) ? "(vacío)" : "*****")} | " +
                    $"DbUserName={company.DbUserName} | " +
                    $"DbPassword={(string.IsNullOrWhiteSpace(company.DbPassword) ? "(vacío)" : "*****")} | " +
                    $"UseTrusted={company.UseTrusted} | " +
                    $"Language={(int)company.language} | " +
                    $"DbServerType={company.DbServerType}"
                );

                throw new InvalidOperationException(
                    $"No fue posible conectar a SAP Business One. Código: {codigo}. Mensaje: {mensaje}");
            }

            return company;
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