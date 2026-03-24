using System;
using System.IO;
using System.Text;

namespace Sincro_Sap_Gosocket.Infraestructura.Logs
{
    internal static class TrazaArchivo
    {
        private static readonly object _lock = new object();
        private static readonly string _carpeta = @"C:\Sincro\Gosocket";

        public static void Escribir(string mensaje)
        {
            try
            {
                Directory.CreateDirectory(_carpeta);

                var archivo = Path.Combine(
                    _carpeta,
                    $"TrazaServicio_{DateTime.Now:yyyyMMdd}.log");

                var linea = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {mensaje}{Environment.NewLine}";

                lock (_lock)
                {
                    File.AppendAllText(archivo, linea, Encoding.UTF8);
                }
            }
            catch
            {
                // no romper el servicio por fallar el log
            }
        }

        public static void Error(string contexto, Exception ex)
        {
            Escribir($"ERROR | {contexto} | {ex}");
        }
    }
}