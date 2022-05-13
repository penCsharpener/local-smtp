using LocalSmtp.Server.Application.Hubs;
using LocalSmtp.Server.Application.Repositories;
using LocalSmtp.Server.Application.Repositories.Abstractions;
using LocalSmtp.Server.Application.Services;
using LocalSmtp.Server.Application.Services.Abstractions;
using LocalSmtp.Server.Infrastructure.Data;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace LocalSmtp.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var assembly = typeof(Program).Assembly;
        var contentRoot = new FileInfo(assembly.Location).DirectoryName;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = assembly.GetName().Name,
            WebRootPath = "wwwroot",
            ContentRootPath = contentRoot,
            Args = args
        });

        ConfigureHost(builder.Host, builder.Configuration, args);

        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Content root: {contentRoot}", contentRoot);

        ConfigureWebApplication(app);

        app.Run();
    }

    private static void ConfigureHost(IHostBuilder host, IConfiguration configuration, string[] args)
    {
        host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(configuration));
        host.ConfigureAppConfiguration((host, configuration) =>
        {
            var homePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalSmtp");

            configuration.Sources.Clear();
            configuration.AddJsonFile("appsettings.json", optional: true, true);
            configuration.AddJsonFile($"appsettings.{host.HostingEnvironment.EnvironmentName}.json", true, true);
            configuration.AddJsonFile(Path.Combine(homePath, "appsettings.json"), optional: true, true);
            configuration.AddUserSecrets<Program>(true, true);
            configuration.AddCommandLine(args);
            configuration.AddEnvironmentVariables();
        });
        host.UseWindowsService();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServerOptions>(configuration.GetSection("ServerOptions"));
        services.Configure<RelayOptions>(configuration.GetSection("RelayOptions"));

        var serverOptions = configuration.GetSection("ServerOptions").Get<ServerOptions>();

        services.AddDbContext<AppDbContext>(opt => opt.UseSqlite($"Data Source='{serverOptions.Database}'"), ServiceLifetime.Scoped, ServiceLifetime.Singleton);
        services.AddSingleton<ILocalSmtpServer, LocalSmtpServer>();
        services.AddSingleton<ImapServer>();
        services.AddScoped<IMessagesRepository, MessagesRepository>();
        services.AddScoped<IHostingEnvironmentHelper, HostingEnvironmentHelper>();
        services.AddSingleton<ITaskQueue, TaskQueue>();
        services.AddCors(opts =>
        {
            opts.AddPolicy("CorsPolicy", policy =>
            {
                policy.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod();
            });
        });

        services.AddSingleton<Func<RelayOptions, SmtpClient>>(relayOptions =>
        {
            if (!relayOptions.IsEnabled)
            {
                return null;
            }

            var result = new SmtpClient();
            result.Connect(relayOptions.SmtpServer, relayOptions.SmtpPort, relayOptions.TlsMode);

            if (!string.IsNullOrEmpty(relayOptions.Login))
            {
                result.Authenticate(relayOptions.Login, relayOptions.Password);
            }

            return result;
        });


        services.AddSignalR();
        services.AddSingleton<NotificationsHub>();

        services.AddControllersWithViews();
        services.AddRazorPages();
    }

    private static void ConfigureWebApplication(WebApplication? app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseBlazorFrameworkFiles();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseWebSockets();

        app.UseRouting();
        app.UseCors("CorsPolicy");
        app.MapHub<NotificationsHub>("/hubs/notifications");

        using (var scope = app.Services.CreateScope())
        {
            using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.Migrate();
        }

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Services.GetRequiredService<ILocalSmtpServer>().TryStart();
        app.Services.GetRequiredService<ImapServer>().TryStart();
    }
}

