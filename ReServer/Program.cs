using ConfigurationEncrypt;
using Figgle.Fonts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReServer.DependencyInjection;
using Serilog;
using SerilogLogger;
using System;
using System.IO;
using System.Reflection;
using WindowsServiceTools;

try
{
    Console.WriteLine("Loading...");

    const string appName = "ReServer";

    var header = $"{appName} {Assembly.GetEntryAssembly()?.GetName().Version}";
    Console.WriteLine(FiggleFonts.Standard.Render(header));

    const string appKey = "CF39BBE3-531B-417E-AC20-3605313D0F94";

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        ContentRootPath = AppContext.BaseDirectory, Args = args
    });

    var debugMode = builder.Environment.IsDevelopment();

    builder.Host.UseSerilogLogger(builder.Configuration, debugMode);
    builder.Host.UseWindowsServiceOnWindows(debugMode, args);

    builder.Configuration.AddConfigurationEncryption(debugMode, appKey);

    // @formatter:off
    builder.Services
        .AddHostedServices(debugMode).AddHttpClient();
    // @formatter:on

    // ReSharper disable once using
    using var app = builder.Build();

    Log.Information("Directory.GetCurrentDirectory() = {0}", Directory.GetCurrentDirectory());
    Log.Information("AppContext.BaseDirectory = {0}", AppContext.BaseDirectory);

    app.Run();

    Log.Information("Finish");
    return 0;
}
catch (Exception e)
{
    Log.Fatal(e, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}