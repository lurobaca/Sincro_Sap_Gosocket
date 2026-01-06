using System;
using System.Net.Http.Headers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
            Host.CreateDefaultBuilder(args)

                .UseWindowsService(options =>
                {
                    options.ServiceName = "Sincro_Sap_Gosocket";
                })

                .ConfigureServices((context, services) =>
                {
                    // 1) Options
                    services.Configure<OpcionesServicio>(context.Configuration.GetSection("ServiceOptions"));
                    services.Configure<OpcionesGosocket>(context.Configuration.GetSection("GoSocket"));
                    services.Configure<OpcionesSql>(context.Configuration.GetSection("ConnectionStrings")); // AJUSTA si tu clase mapea distinto

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

                    // 7) HttpClient GoSocket (TIPADO) - AJUSTADO
                    services.AddHttpClient<IClienteGosocket, ClienteGosocket>((sp, http) =>
                    {
                        var opt = sp.GetRequiredService<IOptions<OpcionesGosocket>>().Value;

                        if (string.IsNullOrWhiteSpace(opt.ApiUrl))
                            throw new InvalidOperationException("Falta GoSocket:ApiUrl en appsettings.json");

                        if (string.IsNullOrWhiteSpace(opt.ApiKey))
                            throw new InvalidOperationException("Falta GoSocket:ApiKey en appsettings.json");

                        if (string.IsNullOrWhiteSpace(opt.Password))
                            throw new InvalidOperationException("Falta GoSocket:Password en appsettings.json");

                        // Normaliza URL base (con / al final)
                        var baseUrl = opt.ApiUrl.Trim();
                        if (!baseUrl.EndsWith("/")) baseUrl += "/";

                        http.BaseAddress = new Uri(baseUrl);

                        // Timeout (ajústalo si quieres)
                        http.Timeout = TimeSpan.FromSeconds(100);

                        // Headers
                        http.DefaultRequestHeaders.Accept.Clear();
                        http.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));

                        // Basic Auth: ApiKey:Password en Base64
                        var raw = $"{opt.ApiKey}:{opt.Password}";
                        var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
                        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", b64);

                        // Opcional: identificador del cliente
                        http.DefaultRequestHeaders.UserAgent.ParseAdd("Sincro_Sap_Gosocket/1.0");
                    });

                    // 8) Worker
                    services.AddHostedService<Worker>();
                })
                .Build()
                .Run();
        }
    }
}
