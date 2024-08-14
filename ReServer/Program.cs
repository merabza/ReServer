using System;
using System.Collections.Generic;
using System.IO;
using ConfigurationEncrypt;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using SwaggerTools;
using WebInstallers;
using AssemblyReference = ApiExceptionHandler.AssemblyReference;

////პროგრამის ატრიბუტების დაყენება 
//ProgramAttributes.Instance.SetAttribute("AppName", "ReServer");
//ProgramAttributes.Instance.SetAttribute("AppKey", "CF39BBE3-531B-417E-AC20-3605313D0F94");
//ProgramAttributes.Instance.SetAttribute("VersionCount", 1);
//ProgramAttributes.Instance.SetAttribute("UseSwaggerWithJWTBearer", false);

try
{
    var parameters = new Dictionary<string, string>
    {
        //{ SignalRMessagesInstaller.SignalRReCounterKey, string.Empty },//Allow SignalRReCounter
        { ConfigurationEncryptInstaller.AppKeyKey, "CF39BBE3-531B-417E-AC20-3605313D0F94" },
        { SwaggerInstaller.AppNameKey, "ReServer" },
        { SwaggerInstaller.VersionCountKey, 1.ToString() }
        //{ SwaggerInstaller.UseSwaggerWithJwtBearerKey, string.Empty },//Allow Swagger
    };

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        ContentRootPath = AppContext.BaseDirectory,
        Args = args
    });

    var debugMode = builder.Environment.IsDevelopment();

    if (!builder.InstallServices(debugMode, args, parameters,

            //WebSystemTools
            AssemblyReference.Assembly,
            ConfigurationEncrypt.AssemblyReference.Assembly,
            HttpClientInstaller.AssemblyReference.Assembly,
            SerilogLogger.AssemblyReference.Assembly,
            SwaggerTools.AssemblyReference.Assembly,
            TestToolsApi.AssemblyReference.Assembly,
            WindowsServiceTools.AssemblyReference.Assembly,

            //ReServer
            ReServer.AssemblyReference.Assembly
        ))
        return 2;

    // ReSharper disable once using
    using var app = builder.Build();

    if (!app.UseServices(debugMode))
        return 3;

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