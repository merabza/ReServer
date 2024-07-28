﻿using System;
using System.Collections.Generic;
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

    public void InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args, Dictionary<string, string> parameters)
    {
        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Started");

        builder.Services.AddSingleton<IProcesses, Processes>();
        builder.Services.AddHostedService<TimedHostedService>();

        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Finished");
    }

    public void UseServices(WebApplication app, bool debugMode)
    {
    }
}