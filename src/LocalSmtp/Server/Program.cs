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

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureHost(builder.Host, builder.Configuration);
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        ConfigureWebApplication(app);

        app.Run();
    }

    private static void ConfigureHost(IHostBuilder host, IConfiguration configuration)
    {
        host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(configuration));
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
        var serverOptions = app.Configuration.GetSection("ServerOptions").Get<ServerOptions>();

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

