// Sincro_Sap_Gosocket/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Aplicacion.Servicios;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Configuracion.OpcionesSql;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket;
using Sincro_Sap_Gosocket.Infraestructura.Sql;
using Sincro_Sap_Gosocket.Options;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http.Headers;

namespace Sincro_Sap_Gosocket
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConfigurarLogging();

            try
            {
                CrearYEjecutarHost(args);
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

        private static void ConfigurarLogging()
        {
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
                    manageEventSource: true,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    logName: "Application")
                .CreateLogger();
        }

        private static void CrearYEjecutarHost(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "Sincro_Sap_Gosocket";
                })
                .UseSerilog()
                .ConfigureServices(ConfigurarServicios)
                .Build()
                .Run();
        }

        private static void ConfigurarServicios(HostBuilderContext context, IServiceCollection services)
        {
            // 1) Options / Config
            RegistrarConfiguraciones(context, services);

            // 2) Validaciones críticas (falla rápido al iniciar)
            ValidarConfiguracionInicial(context);

            // 3) SQL
            RegistrarInfraestructuraSql(context, services);

            // 4) Servicios de aplicación
            RegistrarServiciosAplicacion(services);

            // 5) GoSocket (Basic Auth)
            RegistrarServiciosGoSocket(services);

            // 6) Worker
            services.AddHostedService<Worker>();
        }

        private static void RegistrarConfiguraciones(HostBuilderContext context, IServiceCollection services)
        {
            // GoSocket: Basic Auth (ApiBaseUrl + ApiKey + Password)
            services.Configure<OpcionesGosocket>(context.Configuration.GetSection("GoSocket"));

            services.Configure<OpcionesServicio>(context.Configuration.GetSection("ServiceOptions"));

            // Nota: igual usamos GetConnectionString("Sql"), esto solo deja disponible el bind si usted lo usa en otros lados.
            services.Configure<OpcionesSql>(context.Configuration.GetSection("ConnectionStrings"));
        }

        private static void ValidarConfiguracionInicial(HostBuilderContext context)
        {
            // SQL
            var connectionString = context.Configuration.GetConnectionString("Sql");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Falta ConnectionStrings:Sql en appsettings.json");

            // GoSocket (Basic)
            var opcionesGosocket = context.Configuration.GetSection("GoSocket").Get<OpcionesGosocket>();
            if (opcionesGosocket == null)
            {
                Log.Fatal("No se encontró la configuración de GoSocket en appsettings.json");
                throw new InvalidOperationException("Falta la sección GoSocket en appsettings.json");
            }

            try
            {
                opcionesGosocket.ValidarConfiguracion(exigirOutputPath: false);
                Log.Information("Configuración GoSocket (Basic Auth) validada correctamente");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error en configuración de GoSocket (Basic Auth)");
                throw;
            }
        }

        private static void RegistrarInfraestructuraSql(HostBuilderContext context, IServiceCollection services)
        {
            
            IOptions<OpcionesSql> opcionesSql = services.BuildServiceProvider().GetRequiredService<IOptions<OpcionesSql>>();
            opcionesSql.Value.ConnectionString  = context.Configuration.GetConnectionString("Sql"); ;

            services.AddScoped<ISqlConnectionFactory>(_ => new SqlConnectionFactory(opcionesSql));

            services.AddScoped<IRepositorioColaDocumentos, RepositorioColaDocumentosSql>();
            services.AddScoped<IRepositorioEstados, RepositorioEstadosSql>();
            services.AddScoped<IEjecutorProcedimientos, EjecutorProcedimientosSql>();
        }

        private static void RegistrarServiciosAplicacion(IServiceCollection services)
        {
            services.AddScoped<ITraductorXml, TraductorXml>();
            services.AddScoped<IServicioProcesamientoDocumentos, ServicioProcesamientoDocumentos>();
        }

        private static void RegistrarServiciosGoSocket(IServiceCollection services)
        {
            // Servicio de autenticación: construye Authorization: Basic {base64(ApiKey:Password)}
            // No necesita HttpClient.
            services.AddSingleton<IServicioAutenticacion, ServicioAutenticacion>();

            // Cliente HTTP principal (API v1)
            services.AddHttpClient<IClienteGosocket, ClienteGosocket>((sp, httpClient) =>
            {
                var opciones = sp.GetRequiredService<IOptions<OpcionesGosocket>>().Value;
                opciones.ValidarConfiguracion(exigirOutputPath: false);

                // Importante: ApiBaseUrl debe apuntar a /api/v1/ (según manual).
                var baseUrl = opciones.ApiBaseUrl.EndsWith("/") ? opciones.ApiBaseUrl : opciones.ApiBaseUrl + "/";
                httpClient.BaseAddress = new Uri(baseUrl);

                httpClient.Timeout = TimeSpan.FromSeconds(120);

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                // Headers opcionales de trazabilidad
                httpClient.DefaultRequestHeaders.Remove("X-Client-Version");
                httpClient.DefaultRequestHeaders.Remove("X-Client-Name");
                httpClient.DefaultRequestHeaders.Add("X-Client-Version", "1.0.0");
                httpClient.DefaultRequestHeaders.Add("X-Client-Name", "SincroSapGoSocket");
            });
        }
    }
}
