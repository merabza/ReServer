using System;
using LibToolActions.BackgroundTasks;
using Microsoft.Extensions.DependencyInjection;

namespace ReServer.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class HostedServiceDependencyInjection
{
    public static IServiceCollection AddHostedServices(this IServiceCollection services, bool debugMode)
    {
        if (debugMode)
            Console.WriteLine($"{nameof(AddHostedServices)} Started");

        services.AddSingleton<IProcesses, Processes>();
        services.AddHostedService<TimedHostedService>();

        if (debugMode)
            Console.WriteLine($"{nameof(AddHostedServices)} Finished");

        return services;
    }
}