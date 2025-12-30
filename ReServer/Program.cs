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

//using WebInstallers;
//using AssemblyReference = ApiExceptionHandler.AssemblyReference;

try
{
    Console.WriteLine("Loading...");

    const string appName = "ReServer";
    //const int versionCount = 1;

    var header = $"{appName} {Assembly.GetEntryAssembly()?.GetName().Version}";
    Console.WriteLine(FiggleFonts.Standard.Render(header));

    const string appKey = "CF39BBE3-531B-417E-AC20-3605313D0F94";

    ////პროგრამის ატრიბუტების დაყენება 
    //ProgramAttributes.Instance.AppName = appName;
    //ProgramAttributes.Instance.AppKey = appKey;

    //var parameters = new Dictionary<string, string>
    //{
    //    //{ SignalRMessagesInstaller.SignalRReCounterKey, string.Empty },//Allow SignalRReCounter
    //    { ConfigurationEncryptInstaller.AppKeyKey, appKey },
    //    { SwaggerInstaller.AppNameKey, appName },
    //    { SwaggerInstaller.VersionCountKey, 1.ToString() }
    //    //{ SwaggerInstaller.UseSwaggerWithJwtBearerKey, string.Empty },//Allow Swagger
    //};

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

    //if (!builder.InstallServices(debugMode, args, parameters,

    //        //WebSystemTools
    //        ApiExceptionHandler.AssemblyReference.Assembly,
    //        ConfigurationEncrypt.AssemblyReference.Assembly,
    //        HttpClientInstaller.AssemblyReference.Assembly,
    //        SerilogLogger.AssemblyReference.Assembly,
    //        SwaggerTools.AssemblyReference.Assembly,
    //        TestToolsApi.AssemblyReference.Assembly,
    //        WindowsServiceTools.AssemblyReference.Assembly,

    //        //ReServer
    //        ReServer.AssemblyReference.Assembly))
    //    return 2;

    // ReSharper disable once using
    using var app = builder.Build();

    //if (!app.UseServices(debugMode))
    //    return 3;

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