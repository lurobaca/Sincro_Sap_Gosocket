using System;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Aplicacion.Servicios;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Infraestructura;
using Sincro_Sap_Gosocket.Infraestructura.Sql;
using Sincro_Sap_Gosocket.Options;

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
            // ========== SECCIÓN 1: REGISTRO DE CONFIGURACIONES ==========
            RegistrarConfiguraciones(context, services);

            // ========== SECCIÓN 2: VALIDACIÓN DE CONFIGURACIÓN CRÍTICA ==========
            ValidarConfiguracionInicial(context);

            // ========== SECCIÓN 3: INFRAESTRUCTURA - BASE DE DATOS ==========
            RegistrarInfraestructuraSql(context, services);

            // ========== SECCIÓN 4: SERVICIOS DE APLICACIÓN ==========
            RegistrarServiciosAplicacion(services);

            // ========== SECCIÓN 5: INTEGRACIÓN GOSOCKET API ==========
            RegistrarServiciosGoSocket(services);

            // ========== SECCIÓN 6: WORKER / BACKGROUND SERVICE ==========
            services.AddHostedService<Worker>();
        }

        private static void RegistrarConfiguraciones(HostBuilderContext context, IServiceCollection services)
        {
            services.Configure<OpcionesGosocket>(context.Configuration.GetSection("GoSocket"));
            services.Configure<OpcionesServicio>(context.Configuration.GetSection("ServiceOptions"));
            services.Configure<OpcionesSql>(context.Configuration.GetSection("ConnectionStrings"));
        }

        private static void ValidarConfiguracionInicial(HostBuilderContext context)
        {
            var connectionString = context.Configuration.GetConnectionString("Sql");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Falta ConnectionStrings:Sql en appsettings.json");
            }

            var opcionesGosocket = context.Configuration.GetSection("GoSocket").Get<OpcionesGosocket>();
            if (opcionesGosocket == null)
            {
                Log.Fatal("No se encontró la configuración de GoSocket en appsettings.json");
                throw new InvalidOperationException("Falta la sección GoSocket en appsettings.json");
            }

            try
            {
                opcionesGosocket.ValidarConfiguracion();
                Log.Information("Configuración GoSocket validada correctamente");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error en configuración de GoSocket");
                throw;
            }
        }

        private static void RegistrarInfraestructuraSql(HostBuilderContext context, IServiceCollection services)
        {
            var connectionString = context.Configuration.GetConnectionString("Sql");

            services.AddScoped<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

            // Repositorios SQL
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
            // HttpClient para Servicio de Autenticación (OAuth 2.0)
            services.AddHttpClient<IServicioAutenticacion, ServicioAutenticacion>((serviceProvider, httpClient) =>
            {
                // Este HttpClient es específico para obtener tokens OAuth
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });

            // HttpClient para Cliente Principal de GoSocket
            services.AddHttpClient<IClienteGosocket, ClienteGosocket>((serviceProvider, httpClient) =>
            {
                var opciones = serviceProvider.GetRequiredService<IOptions<OpcionesGosocket>>().Value;

                if (!opciones.UsarOAuth)
                {
                    throw new InvalidOperationException(
                        "La configuración de GoSocket no es válida para OAuth 2.0. " +
                        "Revise las propiedades ClientId, ClientSecret, OAuthTokenUrl y ApiBaseUrl.");
                }

                // Configuración base del HttpClient para el cliente principal
                httpClient.BaseAddress = new Uri(opciones.ApiBaseUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.Timeout = TimeSpan.FromSeconds(120);

                // Headers adicionales para trazabilidad
                httpClient.DefaultRequestHeaders.Add("X-Client-Version", "1.0.0");
                httpClient.DefaultRequestHeaders.Add("X-Client-Name", "SincroSapGoSocket");
            });
        }
    }
}