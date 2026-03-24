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
using Sincro_Sap_Gosocket.Infraestructura.Sap;
using System;
using System.IO;
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


        //private static void CrearYEjecutarHost(string[] args)
        //{
        //    Host.CreateDefaultBuilder(args)
        //        .UseContentRoot(AppContext.BaseDirectory)  
        //        .UseWindowsService(options =>
        //        {
        //            options.ServiceName = "Sincro_Sap_Gosocket";
        //        })
        //        .UseSerilog()
        //        .ConfigureServices(ConfigurarServicios)
        //        .Build()
        //        .Run();
        //}
        private static void CrearYEjecutarHost(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .UseContentRoot(AppContext.BaseDirectory)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "SincroSapGosocket2";
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

            // 4) SAP
            RegistrarServiciosSap(services);

            // 5) Servicios de aplicación
            RegistrarServiciosAplicacion(services);

            // 6) GoSocket (Basic Auth)
            RegistrarServiciosGoSocket(services);

            // 7) Worker
            services.AddHostedService<Worker>();
        }

        private static void RegistrarConfiguraciones(HostBuilderContext context, IServiceCollection services)
        {
            services.Configure<OpcionesGosocket>(context.Configuration.GetSection("GoSocket"));
            services.Configure<OpcionesServicio>(context.Configuration.GetSection("ServiceOptions"));
            services.Configure<OpcionesSap>(context.Configuration.GetSection("Sap"));

            // Importante: NO dependa de bind raro para SQL.
            // Aquí solo dejamos registrado OpcionesSql, pero el valor se setea en RegistrarInfraestructuraSql.
            services.AddOptions<OpcionesSql>();
        }

        //private static void ValidarConfiguracionInicial(HostBuilderContext context)
        //{
        //    // SQL
        //    var connectionString = context.Configuration.GetConnectionString("Sql");
        //    if (string.IsNullOrWhiteSpace(connectionString)) { 
        //        Log.Error("Falta ConnectionStrings:Sql en appsettings.json");
        //        return; 
        //    }
        //    // GoSocket (Basic)
        //    var opcionesGosocket = context.Configuration.GetSection("GoSocket").Get<OpcionesGosocket>();
        //    if (opcionesGosocket == null)
        //    {
        //        Log.Fatal("No se encontró la configuración de GoSocket en appsettings.json");

        //        Log.Error("Falta la sección GoSocket en appsettings.json");
        //        return;
        //    }

        //    try
        //    {
        //        opcionesGosocket.ValidarConfiguracion(exigirOutputPath: false);
        //        Log.Information("Configuración GoSocket (Basic Auth) validada correctamente");
        //    }
        //    catch (Exception ex)
        //    { 
        //        Log.Error("Error en configuración de GoSocket (Basic Auth)");
        //        return;
        //    }

        //    // SAP
        //    var opcionesSap = context.Configuration.GetSection("Sap").Get<OpcionesSap>();
        //    if (opcionesSap == null)
        //    { 
        //        Log.Error("Falta la sección Sap en appsettings.json");
        //        return;
        //    }

        //    if (string.IsNullOrWhiteSpace(opcionesSap.Servidor)) { 
        //        Log.Error("Falta Sap:Servidor en appsettings.json");
        //        return;
        //    }

        //    if (string.IsNullOrWhiteSpace(opcionesSap.LicenciaServidor)) { 
        //        Log.Error("Falta Sap:LicenciaServidor en appsettings.json");
        //        return;
        //    }

        //    if (string.IsNullOrWhiteSpace(opcionesSap.BaseDatos)) { 
        //        Log.Error("Falta Sap:BaseDatos en appsettings.json");
        //        return;
        //    }

        //    if (string.IsNullOrWhiteSpace(opcionesSap.Usuario))
        //    {
        //        Log.Error("Falta Sap:Usuario en appsettings.json");
        //        return;
        //    } 

        //    if (string.IsNullOrWhiteSpace(opcionesSap.Clave))
        //    {
        //        Log.Error("Falta Sap:Clave en appsettings.json");
        //        return;
        //    } 

        //    Log.Information("Configuración SAP validada correctamente");
        //}
        private static void ValidarConfiguracionInicial(HostBuilderContext context)
        {
            var connectionString = context.Configuration.GetConnectionString("Sql");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Falta ConnectionStrings:Sql en appsettings.json");

            var opcionesGosocket = context.Configuration.GetSection("GoSocket").Get<OpcionesGosocket>();
            if (opcionesGosocket == null)
                throw new InvalidOperationException("Falta la sección GoSocket en appsettings.json");

            opcionesGosocket.ValidarConfiguracion(exigirOutputPath: false);

            var opcionesSap = context.Configuration.GetSection("Sap").Get<OpcionesSap>();
            if (opcionesSap == null)
                throw new InvalidOperationException("Falta la sección Sap en appsettings.json");

            if (string.IsNullOrWhiteSpace(opcionesSap.Servidor))
                throw new InvalidOperationException("Falta Sap:Servidor en appsettings.json");

            if (string.IsNullOrWhiteSpace(opcionesSap.LicenciaServidor))
                throw new InvalidOperationException("Falta Sap:LicenciaServidor en appsettings.json");

            if (string.IsNullOrWhiteSpace(opcionesSap.BaseDatos))
                throw new InvalidOperationException("Falta Sap:BaseDatos en appsettings.json");

            if (string.IsNullOrWhiteSpace(opcionesSap.Usuario))
                throw new InvalidOperationException("Falta Sap:Usuario en appsettings.json");

            if (string.IsNullOrWhiteSpace(opcionesSap.Clave))
                throw new InvalidOperationException("Falta Sap:Clave en appsettings.json");

            Log.Information("Configuración inicial validada correctamente");
        }
        private static void RegistrarInfraestructuraSql(HostBuilderContext context, IServiceCollection services)
        {
            var cs = context.Configuration.GetConnectionString("Sql") ?? "";

            // Opción 1 (recomendada): llenar OpcionesSql desde DI sin BuildServiceProvider()
            services.Configure<OpcionesSql>(o => o.ConnectionString = cs);

            services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();

            services.AddScoped<IRepositorioColaDocumentos, RepositorioColaDocumentosSql>();
            services.AddScoped<IRepositorioEstados, RepositorioEstadosSql>();
            services.AddScoped<IEjecutorProcedimientos, EjecutorProcedimientosSql>();
        }

        private static void RegistrarServiciosSap(IServiceCollection services)
        {
            services.AddScoped<ISapConnectionFactory, SapConnectionFactory>();
            services.AddScoped<IServicioActualizacionSap, ServicioActualizacionSap>();
        }

        private static void RegistrarServiciosAplicacion(IServiceCollection services)
        {
            services.AddScoped<ITraductorXml, TraductorXml>();
            services.AddScoped<IServicioProcesamientoDocumentos, ServicioProcesamientoDocumentos>();
        }

        private static void RegistrarServiciosGoSocket(IServiceCollection services)
        {
            services.AddSingleton<IServicioAutenticacion, ServicioAutenticacion>();

            services.AddHttpClient<IClienteGosocket, ClienteGosocket>((sp, httpClient) =>
            {
                var opciones = sp.GetRequiredService<IOptions<OpcionesGosocket>>().Value;
                opciones.ValidarConfiguracion(exigirOutputPath: false);

                var baseUrl = opciones.ApiBaseUrl.EndsWith("/") ? opciones.ApiBaseUrl : opciones.ApiBaseUrl + "/";
                httpClient.BaseAddress = new Uri(baseUrl);

                httpClient.Timeout = TimeSpan.FromSeconds(120);

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });
        }
    }
}