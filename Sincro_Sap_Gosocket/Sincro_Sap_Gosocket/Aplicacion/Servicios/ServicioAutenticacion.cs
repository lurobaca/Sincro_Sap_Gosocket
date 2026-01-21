// Sincro_Sap_Gosocket/Aplicacion/Servicios/ServicioAutenticacion.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;
using System;
using System.Net.Http.Headers;
using System.Text;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    /// <summary>
    /// Servicio de autenticación GoSocket (solo Basic Auth).
    /// Implementa lo requerido por el Manual-API_Cliente:
    ///   Authorization: Basic base64(ApiKey:ApiPassword)
    /// </summary>
    public sealed class ServicioAutenticacion : IServicioAutenticacion
    {
        private readonly OpcionesGosocket _opciones;
        private readonly ILogger<ServicioAutenticacion> _logger;

        public ServicioAutenticacion(
            IOptions<OpcionesGosocket> opciones,
            ILogger<ServicioAutenticacion> logger)
        {
            _opciones = opciones?.Value ?? throw new ArgumentNullException(nameof(opciones));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Valida que existan ApiBaseUrl, ApiKey y ApiPassword.
        /// </summary>
        public void ValidarConfiguracion()
        {
            _opciones.ValidarConfiguracion(exigirOutputPath: false);

            // Validación extra (por claridad en errores)
            if (string.IsNullOrWhiteSpace(_opciones.ApiKey))
                throw new InvalidOperationException("GoSocket:ApiKey es obligatorio (Usuario Basic).");

            if (string.IsNullOrWhiteSpace(_opciones.ApiPassword))
                throw new InvalidOperationException("GoSocket:ApiPassword es obligatorio (Contraseña Basic).");
        }

        /// <summary>
        /// Construye el encabezado Authorization Basic.
        /// </summary>
        public AuthenticationHeaderValue ObtenerEncabezadoAutorizacion()
        {
            // Fallar temprano si falta config
            ValidarConfiguracion();

            // Basic Auth requiere: base64("ApiKey:ApiPassword")
            var credencialesPlano = $"{_opciones.ApiKey}:{_opciones.ApiPassword}";
            var credencialesBytes = Encoding.UTF8.GetBytes(credencialesPlano);
            var credencialesBase64 = Convert.ToBase64String(credencialesBytes);

            _logger.LogDebug("Encabezado Basic Auth generado para GoSocket (ApiKey configurada).");

            return new AuthenticationHeaderValue("Basic", credencialesBase64);
        }
    }
}
