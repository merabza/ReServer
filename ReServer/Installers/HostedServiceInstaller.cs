using System;
using LibToolActions.BackgroundTasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebInstallers;

namespace ReServer.Installers;

// ReSharper disable once UnusedType.Global
public sealed class HostedServiceInstaller : IInstaller
{
    public int InstallPriority => 30;
    public int ServiceUsePriority => 30;

    public void InstallServices(WebApplicationBuilder builder, string[] args)
    {
        Console.WriteLine("HostedServiceInstaller.InstallServices Started");

        builder.Services.AddSingleton<IProcesses, Processes>();
        builder.Services.AddHostedService<TimedHostedService>();

        Console.WriteLine("HostedServiceInstaller.InstallServices Finished");
    }

    public void UseServices(WebApplication app)
    {
    }
}