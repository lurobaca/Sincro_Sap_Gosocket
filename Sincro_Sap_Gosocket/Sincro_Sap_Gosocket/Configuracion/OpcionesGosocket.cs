// Sincro_Sap_Gosocket/Configuracion/OpcionesGosocket.cs
using System;

namespace Sincro_Sap_Gosocket.Configuracion
{
    /// <summary>
    /// Configuración para consumir la API de GoSocket usando Basic Auth,
    /// según Manual-API_Cliente.
    /// </summary>
    public sealed class OpcionesGosocket
    {
        /// <summary>
        /// URL base del API (v1).
        /// Ejemplo Sandbox: https://developers-sbx.gosocket.net/api/v1/
        /// </summary>
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// ApiKey (usuario) mostrado en el portal GoSocket como "Usuario" (Basic/Basic Auth).
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Password mostrado en el portal GoSocket como "Contraseña" (Basic/Basic Auth).
        /// </summary>
        public string ApiPassword { get; set; } = string.Empty;

        /// <summary>
        /// Ruta de salida para archivos (si aplica en su flujo).
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;

        public bool CrearSubcarpetaPorTipo { get; set; }
        public bool CrearSubcarpetaPorFecha { get; set; }

        /// <summary>
        /// Valida la configuración mínima necesaria para operar.
        /// </summary>
        public void ValidarConfiguracion(bool exigirOutputPath = false)
        {
            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
                throw new InvalidOperationException("GoSocket:ApiBaseUrl es obligatorio.");

            if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _))
                throw new InvalidOperationException($"GoSocket:ApiBaseUrl no es una URL válida: {ApiBaseUrl}");

            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new InvalidOperationException("GoSocket:ApiKey es obligatorio (Usuario Basic).");

            if (string.IsNullOrWhiteSpace(ApiPassword))
                throw new InvalidOperationException("GoSocket:ApiPassword es obligatorio (Contraseña Basic).");

            if (exigirOutputPath && string.IsNullOrWhiteSpace(OutputPath))
                throw new InvalidOperationException("GoSocket:OutputPath es obligatorio pero no está configurado.");
        }
    }
}
