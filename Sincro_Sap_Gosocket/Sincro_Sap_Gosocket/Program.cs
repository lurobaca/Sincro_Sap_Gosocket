using System;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.EventLog;

using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Aplicacion.Servicios;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket;
using Sincro_Sap_Gosocket.Infraestructura.Sql;
using Sincro_Sap_Gosocket.Options;

namespace Sincro_Sap_Gosocket
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Ruta recomendada para servicios Windows (permisos estables)
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Sincro_Sap_Gosocket",
                "logs");

            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, "Sincro_Sap_Gosocket-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: logFile,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(2))
                .WriteTo.EventLog(
                    source: "Sincro_Sap_Gosocket",
                    manageEventSource: true, // si da problemas, ponelo en false y crea el Source con PowerShell
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    logName: "Application")
                .CreateLogger();

            try
            {
                Host.CreateDefaultBuilder(args)
                    .UseWindowsService(options =>
                    {
                        options.ServiceName = "Sincro_Sap_Gosocket";
                    })
                    .UseSerilog() // engancha Serilog al ILogger
                    .ConfigureServices((context, services) =>
                    {
                        // 1) Options
                        services.Configure<OpcionesServicio>(context.Configuration.GetSection("ServiceOptions"));
                        services.Configure<OpcionesGosocket>(context.Configuration.GetSection("GoSocket"));
                        services.Configure<OpcionesSql>(context.Configuration.GetSection("ConnectionStrings"));

                        // 2) ConnectionString obligatoria
                        var cs = context.Configuration.GetConnectionString("Sql");
                        if (string.IsNullOrWhiteSpace(cs))
                            throw new InvalidOperationException("Falta ConnectionStrings:Sql en appsettings.json");

                        // 3) SQL Factory / Connection
                        services.AddScoped<ISqlConnectionFactory>(_ => new SqlConnectionFactory(cs));

                        // 4) Repositorios SQL
                        services.AddScoped<IRepositorioColaDocumentos, RepositorioColaDocumentosSql>();
                        services.AddScoped<IRepositorioEstados, RepositorioEstadosSql>();
                        services.AddScoped<IEjecutorProcedimientos, EjecutorProcedimientosSql>();

                        // 5) Traductor
                        services.AddScoped<ITraductorXml, TraductorXml>();

                        // 6) Servicio orquestador
                        services.AddScoped<IServicioProcesamientoDocumentos, ServicioProcesamientoDocumentos>();

                        // 7) HttpClient GoSocket (TIPADO)
                        services.AddHttpClient<IClienteGosocket, ClienteGosocket>((sp, http) =>
                        {
                            var opt = sp.GetRequiredService<IOptions<OpcionesGosocket>>().Value;

                            if (string.IsNullOrWhiteSpace(opt.ApiUrl))
                                throw new InvalidOperationException("Falta GoSocket:ApiUrl en appsettings.json");
                            if (string.IsNullOrWhiteSpace(opt.ApiKey))
                                throw new InvalidOperationException("Falta GoSocket:ApiKey en appsettings.json");
                            if (string.IsNullOrWhiteSpace(opt.Password))
                                throw new InvalidOperationException("Falta GoSocket:Password en appsettings.json");

                            var baseUrl = opt.ApiUrl.Trim();
                            if (!baseUrl.EndsWith("/")) baseUrl += "/";
                            http.BaseAddress = new Uri(baseUrl);

                            http.Timeout = TimeSpan.FromSeconds(100);

                            http.DefaultRequestHeaders.Accept.Clear();
                            http.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));

                            var raw = $"{opt.ApiKey}:{opt.Password}";
                            var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
                            http.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue("Basic", b64);

                            http.DefaultRequestHeaders.UserAgent.ParseAdd("Sincro_Sap_Gosocket/1.0");
                        });

                        // 8) Worker
                        services.AddHostedService<Worker>();
                    })
                    .Build()
                    .Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "El servicio terminó por una excepción no controlada.");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
