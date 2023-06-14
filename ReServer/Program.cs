using System;
using System.IO;
using ConfigurationEncrypt;
using Microsoft.AspNetCore.Builder;
using Serilog;
using SerilogLogger;
using SwaggerTools;
using SystemToolsShared;
using TestToolsMini.Endpoints.V1;
using WebInstallers;
using WindowsServiceTools;

//პროგრამის ატრიბუტების დაყენება 
ProgramAttributes.Instance.SetAttribute("AppName", "ReServer");
ProgramAttributes.Instance.SetAttribute("AppKey", "CF39BBE3-531B-417E-AC20-3605313D0F94");
ProgramAttributes.Instance.SetAttribute("VersionCount", 1);
ProgramAttributes.Instance.SetAttribute("UseSwaggerWithJWTBearer", false);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = AppContext.BaseDirectory,
    Args = args
});

builder.InstallServices(args,
    typeof(Program),
    typeof(TestEndpoints),
    typeof(ConfigurationEncryptInstaller),
    typeof(SerilogLoggerInstaller),
    typeof(UseWindowsServiceInstaller),
    typeof(SwaggerInstaller)
);

var app = builder.Build();

app.UseServices();


try
{
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